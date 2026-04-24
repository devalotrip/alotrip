using Flight.Application.Interfaces;
using Flight.Domain.Repositories;
using Flight.Infrastructure.Auth;
using Flight.Infrastructure.BackgroundJobs;
using Flight.Infrastructure.Cache;
using Flight.Infrastructure.Engines;
using Flight.Infrastructure.Persistence;
using Flight.Infrastructure.Persistence.Interceptors;
using Flight.Infrastructure.Persistence.Repositories;
using Flight.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using Shared.BuildingBlocks.Abstractions;
using Shared.BuildingBlocks.Authentication;

namespace Flight.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration cfg)
    {
        // ── PostgreSQL + EF Core ─────────────────────────────────────────────
        var connStr = cfg.GetConnectionString("Database")
            ?? throw new InvalidOperationException("ConnectionStrings:Database is required.");

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DispatchDomainEventsInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connStr, npg =>
            {
                npg.EnableRetryOnFailure(3);
                npg.CommandTimeout(30);
            });
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // ── Repositories ────────────────────────────────────────────────────
        // BookingRepository implements multiple ISP interfaces — register each.
        services.AddScoped<BookingRepository>();
        services.AddScoped<IBookingRepository>(sp => sp.GetRequiredService<BookingRepository>());
        services.AddScoped<IAgentRepository>(sp => sp.GetRequiredService<BookingRepository>());
        services.AddScoped<IReferenceDataRepository>(sp => sp.GetRequiredService<BookingRepository>());
        services.AddScoped<IBookingAddonRepository>(sp => sp.GetRequiredService<BookingRepository>());
        services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<BookingRepository>());
        services.AddScoped<ISearchAnalyticRepository>(sp => sp.GetRequiredService<BookingRepository>());
        services.AddScoped<IAdminBookingRepository>(sp => sp.GetRequiredService<BookingRepository>());

        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<IAgentConfigRepository, AgentConfigRepository>();

        // ── Geo Repositories ──────────────────────────────────────────────────
        services.AddScoped<IGeoContinentRepository, GeoContinentRepository>();
        services.AddScoped<IGeoCountryRepository, GeoCountryRepository>();
        services.AddScoped<IGeoCityRepository, GeoCityRepository>();
        services.AddScoped<IGeoAirportRepository, GeoAirportRepository>();

        // ── Redis Cache ──────────────────────────────────────────────────────
        var redisConn = cfg.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddStackExchangeRedisCache(opts =>
        {
            opts.Configuration  = redisConn;
            opts.InstanceName   = "flight:";
        });
        services.AddScoped<IFlightCacheService, RedisFlightCacheService>();

        // ── Commission / Service-fee calculator ──────────────────────────────
        // Deprecated: Use ApplyCommissionCommand via MediatR instead
        // services.AddScoped<ICommissionService, CommissionService>();

        // ── Email notifications ──────────────────────────────────────────────
        services.AddScoped<IEmailNotificationService, SmtpEmailNotificationService>();

        // ── Auth (self-hosted JWT — Keycloak-ready) ──────────────────────────
        services.AddHttpContextAccessor();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // ── Flight Engines (tất cả đăng ký IFlightEngine) ───────────────────
        services.AddScoped<IFlightEngine, GalileoEngine>();
        services.AddScoped<IFlightEngine, DatacomEngine>();
        services.AddScoped<IFlightEngine, KiwiEngine>();
        services.AddScoped<IFlightEngine, PkfareEngine>();
        services.AddScoped<IFlightEngine, MaybayEngine>();

        // ── HTTP Clients cho các REST engines ────────────────────────────────
        // Names must match what engines pass to IHttpClientFactory.CreateClient()
        services.AddHttpClient("KiwiWS",    c => c.BaseAddress = new Uri("https://tequila-api.kiwi.com"));
        services.AddHttpClient("PkfareWS",  c => c.BaseAddress = new Uri("https://api.pkfare.com"));
        services.AddHttpClient("MaybayWS",  c => c.BaseAddress = new Uri("https://api.maybay.vn"));
        services.AddHttpClient("Datacom");
        services.AddHttpClient("GalileoWS");
        services.AddHttpClient("OpenExchangeRates");

        // ── Background Jobs ──────────────────────────────────────────────────
        services.AddHostedService<CurrencySyncJob>();
        services.AddHostedService<OutboxProcessorJob>();
        services.AddHostedService<RefreshTokenCleanupJob>();

        // ── Health Checks ────────────────────────────────────────────────────
        services.AddHealthChecks()
            .AddNpgSql(connStr,  name: "postgresql", tags: ["db", "ready"])
            .AddRedis(redisConn, name: "redis",      tags: ["cache", "ready"]);

        return services;
    }

    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        // Auto-migrate khi start
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.MigrateAsync().GetAwaiter().GetResult();

        // Tạo bảng refresh_tokens nếu chưa có (managed bởi JwtTokenService, không dùng EF)
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS refresh_tokens (
                id         uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
                agent_id   int         NOT NULL,
                token      text        NOT NULL,
                expires_at timestamptz NOT NULL,
                revoked_at timestamptz,
                created_at timestamptz NOT NULL DEFAULT now(),
                CONSTRAINT uq_refresh_tokens_token UNIQUE (token)
            );
            CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token    ON refresh_tokens(token);
            CREATE INDEX IF NOT EXISTS idx_refresh_tokens_agent_id ON refresh_tokens(agent_id);
            CREATE INDEX IF NOT EXISTS idx_refresh_tokens_expires  ON refresh_tokens(expires_at)
                WHERE revoked_at IS NULL;
            """);

        return app;
    }
}
