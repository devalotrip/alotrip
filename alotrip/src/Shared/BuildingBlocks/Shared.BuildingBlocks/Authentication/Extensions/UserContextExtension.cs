using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Shared.BuildingBlocks.Authentication.Extensions;

/// <summary>
/// Extension method to build a <see cref="UserContext"/> from the current
/// HTTP context's JWT claims. Works for both self-hosted and Keycloak tokens
/// because both use the same Keycloak-format claim names.
/// </summary>
public static class UserContextExtension
{
    public static UserContext GetCurrentUser(this IHttpContextAccessor accessor)
    {
        var identity = accessor.HttpContext?.User;

        var id       = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? identity?.FindFirst("sub")?.Value
                    ?? string.Empty;

        var userName = identity?.FindFirst(CustomClaimTypes.UserName)?.Value ?? string.Empty;
        var email    = identity?.FindFirst(ClaimTypes.Email)?.Value
                    ?? identity?.FindFirst("email")?.Value
                    ?? string.Empty;
        var tenant   = identity?.FindFirst(CustomClaimTypes.Tenant)?.Value;
        var roles    = identity?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];

        return new UserContext
        {
            Id       = id,
            UserName = userName,
            Email    = email,
            Tenant   = tenant,
            Roles    = roles
        };
    }
}
