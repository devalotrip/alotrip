using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Partners;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.Partners;

public sealed class PartnerAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/partners")
            .WithTags("Admin - Partners")
            //.RequireAuthorization("AdminOnly");

        group.MapGet("/", GetPartners);
        group.MapGet("/{id:int}", GetPartnerById);
        group.MapPost("/", CreatePartner);
        group.MapPut("/{id:int}", UpdatePartner);
        group.MapDelete("/{id:int}", DeletePartner);
    }

    private static async Task<IResult> GetPartners(ISender sender, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPartnersQuery(page, pageSize), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new { total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data }));
    }

    private static async Task<IResult> GetPartnerById(int id, ISender sender, CancellationToken ct = default)
    {
        var item = await sender.Send(new GetPartnerByIdQuery(id), ct);
        return item is null ? Results.NotFound() : Results.Ok(new ApiResponse<PartnerDto>(true, "OK", item));
    }

    private static async Task<IResult> CreatePartner(CreatePartnerRequest req, ISender sender, CancellationToken ct = default)
    {
        var created = await sender.Send(new CreatePartnerCommand(req.Name), ct);
        return Results.Created($"/api/admin/partners/{created.Id}", new ApiResponse<PartnerDto>(true, "Created", created));
    }

    private static async Task<IResult> UpdatePartner(int id, CreatePartnerRequest req, ISender sender, CancellationToken ct = default)
    {
        var updated = await sender.Send(new UpdatePartnerCommand(id, req.Name), ct);
        return updated is null ? Results.NotFound() : Results.Ok(new ApiResponse<PartnerDto>(true, "Updated", updated));
    }

    private static async Task<IResult> DeletePartner(int id, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new DeletePartnerCommand(id), ct);
        return success ? Results.NoContent() : Results.NotFound();
    }
}