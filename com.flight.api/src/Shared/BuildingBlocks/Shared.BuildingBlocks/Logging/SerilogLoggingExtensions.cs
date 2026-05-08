using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.OpenTelemetry;

namespace Shared.BuildingBlocks.Logging;

public static class SerilogLoggingExtensions
{
    public static IServiceCollection AddSerilogLogging(
        this IServiceCollection services, IConfiguration cfg)
    {
        var section = "Serilog";

        var enable = cfg.GetValue($"{section}:Enable", false);
        if (!enable) return services;

        var serviceName  = cfg[$"{section}:ServiceName"] ?? AppDomain.CurrentDomain.FriendlyName;
        var environment  = cfg["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        var otlpEndpoint = cfg[$"{section}:Otlp:Endpoint"];
        var consoleOn    = cfg.GetValue($"{section}:Console:Enable", true);
        var defaultLevel = ParseLevel(cfg[$"{section}:MinimumLevel:Default"] ?? "Information");
        var msLevel      = ParseLevel(cfg[$"{section}:MinimumLevel:Override:Microsoft"] ?? "Warning");
        var sysLevel     = ParseLevel(cfg[$"{section}:MinimumLevel:Override:System"] ?? "Warning");

        var loggerCfg = new LoggerConfiguration()
            .MinimumLevel.Is(defaultLevel)
            .MinimumLevel.Override("Microsoft", msLevel)
            .MinimumLevel.Override("System",    sysLevel)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("service.name",           serviceName)
            .Enrich.WithProperty("service.version",        "1.0.0")
            .Enrich.WithProperty("deployment.environment", environment)
            .Enrich.WithProperty("host.name",              Environment.MachineName)
            .Enrich.WithProperty("process.id",             Environment.ProcessId)
            .Enrich.With<ActivityTraceEnricher>();

        if (consoleOn)
            loggerCfg.WriteTo.Console(new CompactJsonFormatter());

        if (!string.IsNullOrEmpty(otlpEndpoint))
            loggerCfg.WriteTo.OpenTelemetry(o =>
            {
                o.Endpoint  = otlpEndpoint;
                o.Protocol  = OtlpProtocol.Grpc;
                o.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"]           = serviceName,
                    ["deployment.environment"] = environment
                };
            });

        Log.Logger = loggerCfg.CreateLogger();
        services.AddSerilog(Log.Logger, dispose: true);
        return services;
    }

    public static WebApplication UseSerilogReqLogging(this WebApplication app)
    {
        var enable = app.Configuration.GetValue("Serilog:Enable", false);
        if (!enable) return app;

        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (ctx, _, ex) =>
            {
                var path = ctx.Request.Path.Value ?? "";
                if (path.StartsWith("/health",  StringComparison.OrdinalIgnoreCase)) return LogEventLevel.Debug;
                if (path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase)) return LogEventLevel.Debug;
                if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)) return LogEventLevel.Debug;
                return ex != null || ctx.Response.StatusCode >= 500
                    ? LogEventLevel.Error
                    : LogEventLevel.Information;
            };
            options.EnrichDiagnosticContext = (diag, ctx) =>
            {
                diag.Set("RequestHost", ctx.Request.Host.ToString());
                diag.Set("UserAgent",   ctx.Request.Headers.UserAgent.ToString());
                diag.Set("ClientIP",    ctx.Connection.RemoteIpAddress?.ToString());
            };
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0000} ms";
        });

        app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);
        return app;
    }

    private static LogEventLevel ParseLevel(string level)
        => Enum.TryParse<LogEventLevel>(level, true, out var result) ? result : LogEventLevel.Information;
}
