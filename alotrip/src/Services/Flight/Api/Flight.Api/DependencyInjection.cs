using HealthChecks.UI.Client;
using Carter;
using Flight.Application;
using Flight.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Shared.BuildingBlocks.Authentication.Extensions;
using Shared.BuildingBlocks.Exceptions.Handler;
using Shared.BuildingBlocks.Logging;
using System.Threading.RateLimiting;

namespace Flight.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services, IConfiguration cfg)
    {
        // Carter — Minimal API endpoint modules
        services.AddCarter();

        // Authentication + Authorization (dual-mode: self-hosted / Keycloak)
        services.AddAuthenticationAndAuthorization(cfg);

        // Swagger / OpenAPI with Bearer token support
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new()
            {
                Title       = "Flight API",
                Version     = "v1",
                Description = "Flight search, booking and ticketing API — Clean Architecture + DDD"
            });

            // Bearer token security definition
            var bearerScheme = new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                In           = ParameterLocation.Header,
                Description  = "Enter your JWT access token. Example: **Bearer eyJhbGci...**"
            };
            o.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, bearerScheme);

            // Apply globally — Swagger will send the header on all locked endpoints
            o.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = JwtBearerDefaults.AuthenticationScheme
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // Exception handler
        services.AddExceptionHandler<CustomExceptionHandler>();
        services.AddProblemDetails();

        // CORS — configurable via "Cors:AllowedOrigins" in appsettings.json
        services.AddCors(options =>
        {
            options.AddPolicy("FlightApiCors", policy =>
            {
                var allowedOrigins = cfg
                    .GetSection("Cors:AllowedOrigins")
                    .Get<string[]>();

                if (allowedOrigins is { Length: > 0 })
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                else
                    // Dev default: allow any origin (no credentials)
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
            });
        });

        // Rate limiting — protect auth endpoints against brute-force
        // Policy "auth-limit": 10 requests per minute per IP, queue 0 (reject immediately)
        services.AddRateLimiter(opts =>
        {
            opts.OnRejected = async (ctx, ct) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await ctx.HttpContext.Response.WriteAsync(
                    "Too many requests. Please try again later.", ct);
            };

            opts.AddFixedWindowLimiter("auth-limit", o =>
            {
                o.Window            = TimeSpan.FromMinutes(1);
                o.PermitLimit       = 10;
                o.QueueLimit        = 0;
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });
        });

        // Serilog + OpenTelemetry
        services.AddSerilogLogging(cfg);
        services.AddDistributedTracing(cfg);

        return services;
    }

    public static WebApplication UseApi(this WebApplication app)
    {
        // Serilog request logging
        app.UseSerilogReqLogging();

        // Exception handling
        app.UseExceptionHandler();

        // CORS (must come before auth middleware)
        app.UseCors("FlightApiCors");

        // Rate limiter
        app.UseRateLimiter();

        // Swagger (always on for testing; disable in production via app settings if needed)
        app.UseSwagger();
        app.UseSwaggerUI(o =>
        {
            o.SwaggerEndpoint("/swagger/v1/swagger.json", "Flight API v1");
            o.RoutePrefix = "swagger";
        });

        // Auth middleware (order matters: Authentication → Authorization)
        app.UseAuthentication();
        app.UseAuthorization();

        // Health checks
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate      = hc => hc.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // Prometheus metrics
        app.UseObservability();

        // Carter endpoints
        app.MapCarter();

        // Root info endpoint
        app.MapGet("/", () => new
        {
            Service     = "Flight.Api",
            Status      = "Running",
            Version     = "1.0.0",
            Timestamp   = DateTimeOffset.UtcNow,
            Environment = app.Environment.EnvironmentName
        });

        return app;
    }
}
