using Carter;
using Flight.Application.Features.Admin.Commissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.Commissions;

public sealed class AdminCommissionEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/admin/commissions")
            //.RequireAuthorization("AdminOnly")
            .WithTags("Admin - Commissions");

        grp.MapGet("/", HandleListAsync)
            .WithName("AdminListCommissions")
            .WithSummary("Danh sách commissions (có thể filter theo agent_id)");

        grp.MapPost("/", HandleUpsertAsync)
            .WithName("AdminUpsertCommission")
            .WithSummary("Tạo hoặc cập nhật commission (upsert theo agent_id + airline_group + region)");

        grp.MapDelete("/{id:int}", HandleDeleteAsync)
            .WithName("AdminDeleteCommission")
            .WithSummary("Xóa commission theo ID");
    }

    private static async Task<IResult> HandleListAsync(
        ISender sender,
        int? agentId = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetCommissionsQuery(agentId), ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    private static async Task<IResult> HandleUpsertAsync(
        ISender sender,
        UpsertCommissionRequest req,
        CancellationToken ct = default)
    {
        var success = await sender.Send(new UpsertCommissionCommand(req), ct);
        return success
            ? Results.Ok(new ApiResponse<object>(true, "Commission saved.", null))
            : Results.BadRequest(new ApiResponse<object>(false, "Failed to save commission", null));
    }

    private static async Task<IResult> HandleDeleteAsync(
        ISender sender,
        int id,
        CancellationToken ct = default)
    {
        var success = await sender.Send(new DeleteCommissionCommand(id), ct);
        return success
            ? Results.Ok(new ApiResponse<object>(true, "Deleted.", null))
            : Results.NotFound();
    }
}