using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.BuildingBlocks.Logging;

public static class DistributedTracingExtensions
{
    public static IServiceCollection AddDistributedTracing(
        this IServiceCollection services, IConfiguration cfg)
    {
        var section = "DistributedTracing";
        var enable  = cfg.GetValue($"{section}:Enable", false);
        if (!enable) return services;

        var serviceName  = cfg[$"{section}:ServiceName"] ?? AppDomain.CurrentDomain.FriendlyName;
        var otlpEndpoint = cfg[$"{section}:Otlp:Endpoint"] ?? "http://otel-collector:4317";
        var samplingRate = cfg.GetValue($"{section}:SamplingRate", 1.0);
        var environment  = cfg["ASPNETCORE_ENVIRONMENT"] ?? "Production";

        services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(serviceName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment,
                    ["host.name"]              = Environment.MachineName
                }))
            .WithTracing(t => t
                .SetSampler(new TraceIdRatioBasedSampler(samplingRate))
                .AddAspNetCoreInstrumentation(o =>
                {
                    o.RecordException = true;
                    o.Filter = ctx =>
                    {
                        var p = ctx.Request.Path.Value ?? "";
                        return !p.StartsWith("/health") && !p.StartsWith("/metrics");
                    };
                })
                .AddHttpClientInstrumentation(o => o.RecordException = true)
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(otlpEndpoint);
                    o.Protocol = OtlpExportProtocol.Grpc;
                }))
            .WithMetrics(m => m
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(otlpEndpoint);
                    o.Protocol = OtlpExportProtocol.Grpc;
                })
                .AddPrometheusExporter());

        return services;
    }

    public static WebApplication UseObservability(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint();
        return app;
    }
}
