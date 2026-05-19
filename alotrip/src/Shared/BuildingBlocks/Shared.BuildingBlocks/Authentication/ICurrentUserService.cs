namespace Shared.BuildingBlocks.Authentication;

/// <summary>
/// Interface for accessing the current authenticated user.
/// Implemented by CurrentUserService (via IHttpContextAccessor).
/// Abstracted so it can be mocked in unit tests and swapped between
/// self-hosted JWT and Keycloak without changing callers.
/// </summary>
public interface ICurrentUserService
{
    UserContext GetCurrentUser();
    bool IsAuthenticated { get; }
}
