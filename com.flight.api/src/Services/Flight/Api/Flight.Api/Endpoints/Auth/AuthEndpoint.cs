using Carter;
using Flight.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Auth;

/// <summary>
/// Auth endpoints — self-hosted JWT (Keycloak-ready).
///
/// POST   /api/auth/token    — login with agentCode + password → token pair
/// POST   /api/auth/refresh  — rotate refresh token → new token pair
/// DELETE /api/auth/token    — revoke refresh token (logout)
///
/// Keycloak migration: remove this file and LoginCommand.
/// Keycloak issues tokens directly; these routes are replaced by Keycloak UI.
/// </summary>
public sealed class AuthEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/token", HandleLoginAsync)
            .WithName("Login")
            .WithSummary("Đăng nhập — trả về access token + refresh token")
            .RequireRateLimiting("auth-limit")
            .AllowAnonymous()
            .Produces<ApiResponse<TokenResponse>>(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(401)
            .Produces(429);

        group.MapPost("/refresh", HandleRefreshAsync)
            .WithName("RefreshToken")
            .WithSummary("Làm mới access token bằng refresh token")
            .RequireRateLimiting("auth-limit")
            .AllowAnonymous()
            .Produces<ApiResponse<TokenResponse>>(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(401)
            .Produces(429);

        group.MapDelete("/token", HandleRevokeAsync)
            .WithName("Logout")
            .WithSummary("Đăng xuất — thu hồi refresh token")
            .RequireAuthorization()
            .Produces(204)
            .Produces<ProblemDetails>(400);
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private static async Task<IResult> HandleLoginAsync(
        ISender sender,
        LoginRequest request,
        CancellationToken ct = default)
    {
        var command = new LoginCommand(request.AgentCode, request.Password);
        var result  = await sender.Send(command, ct);
        return Results.Ok(new ApiResponse<TokenResponse>(
            true, "Login successful.",
            new TokenResponse(result.AccessToken, result.RefreshToken, result.ExpiresInSeconds, result.TokenType)));
    }

    private static async Task<IResult> HandleRefreshAsync(
        ISender sender,
        RefreshRequest request,
        CancellationToken ct = default)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result  = await sender.Send(command, ct);
        return Results.Ok(new ApiResponse<TokenResponse>(
            true, "Token refreshed.",
            new TokenResponse(result.AccessToken, result.RefreshToken, result.ExpiresInSeconds, result.TokenType)));
    }

    private static async Task<IResult> HandleRevokeAsync(
        ISender sender,
        RevokeRequest request,
        CancellationToken ct = default)
    {
        var command = new RevokeTokenCommand(request.RefreshToken);
        await sender.Send(command, ct);
        return Results.NoContent();
    }

    // ── Request / Response DTOs ───────────────────────────────────────────────

    public sealed record LoginRequest(string AgentCode, string Password);

    public sealed record RefreshRequest(string RefreshToken);

    public sealed record RevokeRequest(string RefreshToken);

    public sealed record TokenResponse(
        string AccessToken,
        string RefreshToken,
        int    ExpiresInSeconds,
        string TokenType);
}
