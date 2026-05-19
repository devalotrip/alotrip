using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.UserAccounts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.UserAccounts;

public sealed class UserAccountAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users")
            .WithTags("Admin - User Accounts")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", GetUsers);
        group.MapGet("/{id:int}", GetUserById);
        group.MapPost("/", CreateUser);
        group.MapPut("/{id:int}", UpdateUser);
        group.MapDelete("/{id:int}", DeleteUser);
        group.MapPut("/{id:int}/password", ChangeUserPassword);
    }

    private static async Task<IResult> GetUsers(
        ISender sender,
        int page = 1,
        int pageSize = 20,
        string? email = null,
        bool? active = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetUsersQuery(page, pageSize, email, active), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new { total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data }));
    }

    private static async Task<IResult> GetUserById(int id, ISender sender, CancellationToken ct = default)
    {
        var user = await sender.Send(new GetUserByIdQuery(id), ct);
        return user is null ? Results.NotFound(new ApiResponse<object>(false, "User not found", null)) : Results.Ok(new ApiResponse<UserAccountDto>(true, "OK", user));
    }

    private static async Task<IResult> CreateUser(CreateUserAccountRequest req, ISender sender, CancellationToken ct = default)
    {
        try
        {
            var created = await sender.Send(new CreateUserCommand(req.UserRoleId ?? 0, req.Email, req.Password, req.Phone, req.FullName, req.Gender, req.Address, req.Active), ct);
            return Results.Created($"/api/admin/users/{created.Id}", new ApiResponse<UserAccountDto>(true, "Created", created));
        }
        catch (InvalidOperationException)
        {
            return Results.BadRequest(new ApiResponse<object>(false, "Email already exists", null));
        }
    }

    private static async Task<IResult> UpdateUser(int id, UpdateUserAccountRequest req, ISender sender, CancellationToken ct = default)
    {
        var updated = await sender.Send(new UpdateUserCommand(id, req.UserRoleId ?? 0, req.Phone, req.FullName, req.Gender, req.Address, req.Active, req.OTP), ct);
        return updated is null ? Results.NotFound(new ApiResponse<object>(false, "User not found", null)) : Results.Ok(new ApiResponse<UserAccountDto>(true, "Updated", updated));
    }

    private static async Task<IResult> DeleteUser(int id, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new DeleteUserCommand(id), ct);
        return success ? Results.NoContent() : Results.NotFound(new ApiResponse<object>(false, "User not found", null));
    }

    private static async Task<IResult> ChangeUserPassword(int id, ChangeUserPasswordRequest req, ISender sender, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.NewPassword))
            return Results.BadRequest(new ApiResponse<object>(false, "NewPassword is required.", null));
        var success = await sender.Send(new ChangeUserPasswordCommand(id, req.NewPassword), ct);
        return success
            ? Results.Ok(new ApiResponse<object>(true, "Password changed.", null))
            : Results.NotFound(new ApiResponse<object>(false, "User not found", null));
    }
}

public sealed class ChangeUserPasswordRequest
{
    public string NewPassword { get; set; } = default!;
}