using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Insurances;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.Insurances;

public sealed class InsuranceAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/insurances")
            .WithTags("Admin - Insurances")
            //.RequireAuthorization("AdminOnly");

        group.MapGet("/", GetInsurances);
        group.MapGet("/{id:int}", GetInsuranceById);
        group.MapPost("/", CreateInsurance);
        group.MapDelete("/{id:int}", DeleteInsurance);
    }

    private static async Task<IResult> GetInsurances(ISender sender, int page = 1, int pageSize = 20, string? bookingId = null, CancellationToken ct = default)
    {
        Guid? parsedBookingId = string.IsNullOrEmpty(bookingId) ? null : Guid.Parse(bookingId);
        var result = await sender.Send(new GetInsurancesQuery(page, pageSize, parsedBookingId), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new { total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data }));
    }

    private static async Task<IResult> GetInsuranceById(int id, ISender sender, CancellationToken ct = default)
    {
        var item = await sender.Send(new GetInsuranceByIdQuery(id), ct);
        return item is null ? Results.NotFound(new ApiResponse<object>(false, "Insurance not found", null)) : Results.Ok(new ApiResponse<InsuranceDto>(true, "OK", item));
    }

    private static async Task<IResult> CreateInsurance(CreateInsuranceRequest req, ISender sender, CancellationToken ct = default)
    {
        var created = await sender.Send(new CreateInsuranceCommand(req.BookingId, req.PassengerId, req.Provider, req.PolicyNumber, req.CoverageType, req.Premium, req.Currency, req.StartDate, req.EndDate, req.BeneficiaryName, req.BeneficiaryPhone), ct);
        return Results.Created($"/api/admin/insurances/{created.Id}", new ApiResponse<InsuranceDto>(true, "Created", created));
    }

    private static async Task<IResult> DeleteInsurance(int id, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new DeleteInsuranceCommand(id), ct);
        return success ? Results.NoContent() : Results.NotFound(new ApiResponse<object>(false, "Insurance not found", null));
    }
}