namespace Shared.Common.Configurations;

/// <summary>
/// Strongly-typed binding for the "Authentication" section in appsettings.json.
///
/// Self-hosted mode (Authority is null/empty):
///   Secret, Issuer, Audience, AccessTokenExpiryMinutes, RefreshTokenExpiryDays
///
/// Keycloak mode (Authority is set):
///   Authority, Audience, RequireHttpsMetadata
///   (Secret / Issuer are unused — Keycloak provides JWKS endpoint)
/// </summary>
public sealed class AuthenticationCfg
{
    public const string Section = "Authentication";

    // ── Keycloak mode ────────────────────────────────────────────────────────
    /// <summary>Keycloak realm URL, e.g. http://keycloak:8080/realms/flight</summary>
    public string? Authority { get; init; }

    /// <summary>OAuth2 ClientId used in Swagger UI OAuth2 flow</summary>
    public string? ClientId { get; init; }

    public bool RequireHttpsMetadata { get; init; } = false;

    // ── Self-hosted JWT mode ─────────────────────────────────────────────────
    /// <summary>HMAC-SHA256 secret — must be ≥32 chars. Required when Authority is absent.</summary>
    public string? Secret { get; init; }

    /// <summary>JWT iss claim value (default: "flight-api")</summary>
    public string Issuer { get; init; } = "flight-api";

    /// <summary>JWT aud claim value (default: "flight-api")</summary>
    public string Audience { get; init; } = "flight-api";

    /// <summary>Access token lifetime in minutes (default: 15)</summary>
    public int AccessTokenExpiryMinutes { get; init; } = 15;

    /// <summary>Refresh token lifetime in days (default: 7)</summary>
    public int RefreshTokenExpiryDays { get; init; } = 7;
}
