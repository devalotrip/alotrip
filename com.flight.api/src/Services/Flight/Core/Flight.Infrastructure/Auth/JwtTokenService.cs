using Flight.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Shared.Common.Configurations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Flight.Infrastructure.Auth;

/// <summary>
/// Self-hosted JWT token service.
///
/// Login flow:
///   1. Query "agents" table by agent_code.
///   2. Verify password: MD5(input) == stored hash (legacy system).
///   3. Issue access token (Keycloak-format claims) + opaque refresh token.
///   4. Persist refresh token in "refresh_tokens" table (raw ADO.NET).
///
/// Keycloak migration: delete this file and LoginCommand; no other changes needed.
/// </summary>
public sealed class JwtTokenService(IConfiguration cfg) : IJwtTokenService
{
    private readonly AuthenticationCfg _authCfg =
        cfg.GetSection(AuthenticationCfg.Section).Get<AuthenticationCfg>()
        ?? throw new InvalidOperationException("Authentication config section is missing.");

    private readonly string _connStr =
        cfg.GetConnectionString("Database")
        ?? throw new InvalidOperationException("ConnectionStrings:Database is missing.");

    // ── Login ────────────────────────────────────────────────────────────────
    public async Task<TokenResultDto?> LoginAsync(
        string agentCode, string password, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync(ct);

        const string sql = """
            SELECT id, agent_code, email, password_hash, active
            FROM agents
            WHERE agent_code = @code
            LIMIT 1
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("code", agentCode);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var agentId      = reader.GetInt32(0);
        var storedCode   = reader.GetString(1);
        var email        = reader.IsDBNull(2) ? "" : reader.GetString(2);
        var passwordHash = reader.IsDBNull(3) ? "" : reader.GetString(3);
        var isActive     = reader.GetBoolean(4);

        await reader.CloseAsync();

        if (!isActive) return null;
        if (!VerifyMd5(password, passwordHash)) return null;

        var roles = await GetAgentRolesAsync(conn, agentId, ct);
        var (accessToken, expiresIn) = IssueAccessToken(agentId, storedCode, email, roles);
        var refreshToken = await CreateRefreshTokenAsync(conn, agentId, ct);

        return new TokenResultDto(accessToken, refreshToken, expiresIn);
    }

    // ── Refresh ──────────────────────────────────────────────────────────────
    public async Task<TokenResultDto?> RefreshAsync(
        string refreshToken, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync(ct);

        const string sql = """
            SELECT rt.id, rt.agent_id, rt.expires_at, rt.revoked_at,
                   a.agent_code, a.email, a.active
            FROM refresh_tokens rt
            JOIN agents a ON a.id = rt.agent_id
            WHERE rt.token = @token
            LIMIT 1
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("token", refreshToken);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var tokenId   = reader.GetGuid(0);
        var agentId   = reader.GetInt32(1);
        var expiresAt = reader.GetDateTime(2).ToUniversalTime();
        var revokedAt = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3).ToUniversalTime();
        var agentCode = reader.GetString(4);
        var email     = reader.IsDBNull(5) ? "" : reader.GetString(5);
        var isActive  = reader.GetBoolean(6);

        await reader.CloseAsync();

        if (revokedAt is not null || expiresAt < DateTime.UtcNow || !isActive)
            return null;

        // Revoke old token
        await RevokeByIdAsync(conn, tokenId, ct);

        var roles = await GetAgentRolesAsync(conn, agentId, ct);
        var (accessToken, expiresIn) = IssueAccessToken(agentId, agentCode, email, roles);
        var newRefresh = await CreateRefreshTokenAsync(conn, agentId, ct);

        return new TokenResultDto(accessToken, newRefresh, expiresIn);
    }

    // ── Revoke ───────────────────────────────────────────────────────────────
    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync(ct);

        const string sql = """
            UPDATE refresh_tokens
            SET revoked_at = NOW()
            WHERE token = @token AND revoked_at IS NULL
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("token", refreshToken);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private (string token, int expiresIn) IssueAccessToken(
        int agentId, string agentCode, string email, IEnumerable<string> roles)
    {
        if (string.IsNullOrWhiteSpace(_authCfg.Secret))
            throw new InvalidOperationException("Authentication:Secret is required for self-hosted JWT.");

        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authCfg.Secret));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_authCfg.AccessTokenExpiryMinutes);

        // Build realm_access.roles claim in Keycloak format
        var rolesJson = System.Text.Json.JsonSerializer.Serialize(new { roles });

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,    agentId.ToString()),
            new(JwtRegisteredClaimNames.Jti,    Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                                                ClaimValueTypes.Integer64),
            new("preferred_username",           agentCode),
            new(ClaimTypes.Email,               email),
            new("realm_access",                 rolesJson,
                                                "JSON")          // Keycloak-format roles blob
        };

        var token = new JwtSecurityToken(
            issuer:             _authCfg.Issuer,
            audience:           _authCfg.Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expires,
            signingCredentials: creds);

        var expiresIn = _authCfg.AccessTokenExpiryMinutes * 60;
        return (new JwtSecurityTokenHandler().WriteToken(token), expiresIn);
    }

    private async Task<string> CreateRefreshTokenAsync(
        NpgsqlConnection conn, int agentId, CancellationToken ct)
    {
        var token     = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresAt = DateTime.UtcNow.AddDays(_authCfg.RefreshTokenExpiryDays);

        const string sql = """
            INSERT INTO refresh_tokens (agent_id, token, expires_at)
            VALUES (@agentId, @token, @expires)
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("agentId", agentId);
        cmd.Parameters.AddWithValue("token",   token);
        cmd.Parameters.AddWithValue("expires", expiresAt);
        await cmd.ExecuteNonQueryAsync(ct);

        return token;
    }

    private static async Task RevokeByIdAsync(
        NpgsqlConnection conn, Guid tokenId, CancellationToken ct)
    {
        const string sql = """
            UPDATE refresh_tokens SET revoked_at = NOW() WHERE id = @id
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", tokenId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<List<string>> GetAgentRolesAsync(
        NpgsqlConnection conn, int agentId, CancellationToken ct)
    {
        // All agents currently get the "agent" role.
        // Extend this to query an agent_roles table when multi-role support is added.
        await Task.CompletedTask;
        return ["agent"];
    }

    private static bool VerifyMd5(string input, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;

        // Support plain-text passwords stored in old system (no hash)
        if (storedHash.Length != 32)
            return string.Equals(input, storedHash, StringComparison.Ordinal);

        var inputHash = ComputeMd5(input);
        return string.Equals(inputHash, storedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeMd5(string input)
    {
        var bytes  = Encoding.UTF8.GetBytes(input);
        var hash   = MD5.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
