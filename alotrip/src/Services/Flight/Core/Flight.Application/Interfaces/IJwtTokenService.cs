namespace Flight.Application.Interfaces;

/// <summary>
/// Contract for JWT token operations — used by self-hosted LoginCommand.
/// When migrating to Keycloak: delete this interface and LoginCommand,
/// tokens are issued by Keycloak directly.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Validates credentials against the agents table and issues a token pair.
    /// Returns null if credentials are invalid.
    /// </summary>
    Task<TokenResultDto?> LoginAsync(string agentCode, string password, CancellationToken ct = default);

    /// <summary>
    /// Rotates a refresh token. Old token is revoked; new pair is issued.
    /// Returns null if the refresh token is invalid, expired, or already revoked.
    /// </summary>
    Task<TokenResultDto?> RefreshAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Revokes a refresh token (logout).</summary>
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
}

/// <summary>Token pair returned by login and refresh operations.</summary>
public sealed record TokenResultDto(
    string AccessToken,
    string RefreshToken,
    int    ExpiresInSeconds,
    string TokenType = "Bearer"
);
