namespace Shared.BuildingBlocks.Authentication;

/// <summary>
/// Represents the currently authenticated user, parsed from JWT claims.
/// Compatible with both self-hosted JWT and Keycloak tokens.
/// </summary>
public sealed class UserContext
{
    /// <summary>Agent ID — maps to JWT claim "sub"</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Agent code — maps to JWT claim "preferred_username"</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Email — maps to JWT claim "email"</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Tenant identifier — maps to JWT claim "tenant" (optional)</summary>
    public string? Tenant { get; init; }

    /// <summary>Roles extracted from realm_access.roles</summary>
    public List<string> Roles { get; init; } = [];

    public bool IsAuthenticated => !string.IsNullOrEmpty(Id);

    public bool HasRole(string role) =>
        Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
}
