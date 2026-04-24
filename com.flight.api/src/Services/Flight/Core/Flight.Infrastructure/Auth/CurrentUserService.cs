using Microsoft.AspNetCore.Http;
using Shared.BuildingBlocks.Authentication;
using Shared.BuildingBlocks.Authentication.Extensions;

namespace Flight.Infrastructure.Auth;

/// <summary>
/// Implements ICurrentUserService by reading claims from the current HTTP context.
/// The underlying GetCurrentUser() extension works for both self-hosted JWT
/// and Keycloak tokens — both produce the same Keycloak-format claims.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public UserContext GetCurrentUser() => accessor.GetCurrentUser();

    public bool IsAuthenticated =>
        accessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
}
