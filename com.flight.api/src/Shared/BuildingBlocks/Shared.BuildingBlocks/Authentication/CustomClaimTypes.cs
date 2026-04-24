namespace Shared.BuildingBlocks.Authentication;

/// <summary>
/// JWT claim type constants — matching Keycloak claim names so the same
/// token format works for both self-hosted and Keycloak modes.
/// </summary>
public static class CustomClaimTypes
{
    /// <summary>Keycloak realm_access object: { "roles": ["agent","admin"] }</summary>
    public const string RealmAccess = "realm_access";

    /// <summary>Property inside realm_access that holds the roles array</summary>
    public const string Roles = "roles";

    /// <summary>Keycloak preferred_username — used as agent code</summary>
    public const string UserName = "preferred_username";

    /// <summary>email_verified flag from Keycloak</summary>
    public const string EmailVerified = "email_verified";

    /// <summary>Custom tenant claim for multi-tenant support</summary>
    public const string Tenant = "tenant";
}
