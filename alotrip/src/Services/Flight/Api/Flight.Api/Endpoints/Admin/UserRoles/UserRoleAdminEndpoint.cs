using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.UserRoles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.UserRoles;

public sealed class UserRoleAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/user-roles")
            .WithTags("Admin - User Roles")
            //.RequireAuthorization("AdminOnly");

        group.MapGet("/", GetUserRoles);
        group.MapGet("/{id:int}", GetUserRoleById);
        group.MapPost("/", CreateUserRole);
        group.MapDelete("/{id:int}", DeleteUserRole);
    }

    private static async Task<IResult> GetUserRoles(ISender sender, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetUserRolesQuery(page, pageSize), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new { total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data }));
    }

    private static async Task<IResult> GetUserRoleById(int id, ISender sender, CancellationToken ct = default)
    {
        var item = await sender.Send(new GetUserRoleByIdQuery(id), ct);
        return item is null ? Results.NotFound() : Results.Ok(new ApiResponse<UserRoleDto>(true, "OK", item));
    }

    private static async Task<IResult> CreateUserRole(CreateUserRoleRequest req, ISender sender, CancellationToken ct = default)
    {
        var created = await sender.Send(new CreateUserRoleCommand(req.Name, req.Description), ct);
        return Results.Created($"/api/admin/user-roles/{created.Id}", new ApiResponse<UserRoleDto>(true, "Created", created));
    }

    private static async Task<IResult> DeleteUserRole(int id, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new DeleteUserRoleCommand(id), ct);
        return success ? Results.NoContent() : Results.NotFound();
    }
}