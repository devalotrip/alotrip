using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Shared.BuildingBlocks.Authentication.Extensions;

/// <summary>
/// Registers JWT Bearer authentication + authorization policies.
///
/// DUAL MODE:
///   • Keycloak mode  — when "Authentication:Authority" is set in config.
///                      Uses Authority URL for token metadata discovery.
///   • Self-hosted    — when Authority is absent.
///                      Uses "Authentication:Secret" as SymmetricSecurityKey.
///
/// In both modes the same OnTokenValidated handler parses realm_access.roles
/// so that ClaimTypes.Role is always populated for policy evaluation.
///
/// MIGRATION PATH TO KEYCLOAK:
///   1. Remove JwtTokenService, LoginCommand, AuthEndpoint.
///   2. Set Authentication:Authority in config.
///   3. No other code changes needed.
/// </summary>
public static class AuthorizationExtension
{
    public static IServiceCollection AddAuthenticationAndAuthorization(
        this IServiceCollection services,
        IConfiguration cfg)
    {
        var section   = cfg.GetSection("Authentication");
        var authority = section["Authority"];
        var audience  = section["Audience"] ?? "flight-api";
        var issuer    = section["Issuer"]   ?? "flight-api";
        var secret    = section["Secret"];
        bool.TryParse(section["RequireHttpsMetadata"], out var requireHttps);

        var authBuilder = services.AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        });

        if (!string.IsNullOrWhiteSpace(authority))
        {
            // ── Keycloak mode ────────────────────────────────────────────────
            authBuilder.AddJwtBearer(opt =>
            {
                opt.Authority             = authority;
                opt.Audience              = audience;
                opt.RequireHttpsMetadata  = requireHttps;
                opt.SaveToken             = true;

                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = false,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey  = true,
                    ClockSkew                = TimeSpan.FromSeconds(60),
                    NameClaimType            = JwtRegisteredClaimNames.Sub,
                    RoleClaimType            = ClaimTypes.Role
                };

                opt.Events = BuildTokenValidatedEvents();
            });
        }
        else
        {
            // ── Self-hosted mode ─────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException(
                    "Authentication:Secret must be set when Authority is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            authBuilder.AddJwtBearer(opt =>
            {
                opt.RequireHttpsMetadata = false;
                opt.SaveToken            = true;

                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidIssuer              = issuer,
                    ValidateAudience         = true,
                    ValidAudience            = audience,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey  = true,
                    IssuerSigningKey         = key,
                    ClockSkew                = TimeSpan.FromSeconds(60),
                    NameClaimType            = JwtRegisteredClaimNames.Sub,
                    RoleClaimType            = ClaimTypes.Role
                };

                opt.Events = BuildTokenValidatedEvents();
            });
        }

        // ── Authorization policies ───────────────────────────────────────────
        services.AddAuthorization(opt =>
        {
            opt.AddPolicy("AgentOnly", p => p.RequireRole("agent", "admin"));
            opt.AddPolicy("AdminOnly", p => p.RequireRole("admin"));
        });

        return services;
    }

    // ── Shared OnTokenValidated: parse realm_access.roles → ClaimTypes.Role ──
    private static JwtBearerEvents BuildTokenValidatedEvents() => new()
    {
        OnTokenValidated = ctx =>
        {
            if (ctx.Principal?.Identity is not ClaimsIdentity identity)
                return Task.CompletedTask;

            var realmAccess = ctx.Principal.FindFirst(CustomClaimTypes.RealmAccess)?.Value;
            if (realmAccess is null) return Task.CompletedTask;

            try
            {
                using var doc = JsonDocument.Parse(realmAccess);
                if (doc.RootElement.TryGetProperty(CustomClaimTypes.Roles, out var roles))
                {
                    foreach (var r in roles.EnumerateArray())
                    {
                        var role = r.GetString();
                        if (!string.IsNullOrEmpty(role))
                            identity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }
                }
            }
            catch
            {
                // Malformed realm_access — skip silently; auth still works without roles
            }

            return Task.CompletedTask;
        }
    };
}
