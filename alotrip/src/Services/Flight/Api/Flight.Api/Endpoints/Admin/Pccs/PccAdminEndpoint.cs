using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Pccs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.Pccs;

public sealed class PccAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/pccs")
            .WithTags("Admin - PCCs")
            //.RequireAuthorization("AdminOnly");

        group.MapGet("/", GetPccs);
        group.MapGet("/{pcc}", GetPccById);
        group.MapPost("/", CreatePcc);
        group.MapDelete("/{pcc}", DeletePcc);
    }

    private static async Task<IResult> GetPccs(ISender sender, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPccsQuery(page, pageSize), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new { total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data }));
    }

    private static async Task<IResult> GetPccById(string pcc, ISender sender, CancellationToken ct = default)
    {
        var item = await sender.Send(new GetPccByIdQuery(pcc), ct);
        return item is null ? Results.NotFound() : Results.Ok(new ApiResponse<PccDto>(true, "OK", item));
    }

    private static async Task<IResult> CreatePcc(CreatePccRequest req, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new CreatePccCommand(req.Pcc), ct);
        if (!success) return Results.BadRequest(new ApiResponse<object>(false, "Already exists", null));
        var created = await sender.Send(new GetPccByIdQuery(req.Pcc), ct);
        return Results.Created($"/api/admin/pccs/{req.Pcc}", new ApiResponse<PccDto>(true, "Created", created));
    }

    private static async Task<IResult> DeletePcc(string pcc, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new DeletePccCommand(pcc), ct);
        return success ? Results.NoContent() : Results.NotFound();
    }
}