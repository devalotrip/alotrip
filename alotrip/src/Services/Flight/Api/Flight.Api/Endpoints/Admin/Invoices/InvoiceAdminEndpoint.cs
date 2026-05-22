using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.Admin.Invoices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.Invoices;

public sealed class InvoiceAdminEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/invoices")
            .WithTags("Admin - Invoices");
		//.RequireAuthorization("AdminOnly");

		group.MapGet("/", GetInvoices);
        group.MapGet("/{id:int}", GetInvoiceById);
        group.MapPost("/", CreateInvoice);
        group.MapPut("/{id:int}", UpdateInvoice);
        group.MapDelete("/{id:int}", DeleteInvoice);
    }

    private static async Task<IResult> GetInvoices(ISender sender, int page = 1, int pageSize = 20, string? bookingId = null, CancellationToken ct = default)
    {
        Guid? parsedBookingId = string.IsNullOrEmpty(bookingId) ? null : Guid.Parse(bookingId);
        var result = await sender.Send(new GetInvoicesQuery(page, pageSize, parsedBookingId), ct);
        return Results.Ok(new ApiResponse<object>(true, "OK", new { total = result.Total, Page = result.Page, PageSize = result.PageSize, Data = result.Data }));
    }

    private static async Task<IResult> GetInvoiceById(int id, ISender sender, CancellationToken ct = default)
    {
        var invoice = await sender.Send(new GetInvoiceByIdQuery(id), ct);
        return invoice is null ? Results.NotFound(new ApiResponse<object>(false, "Invoice not found", null)) : Results.Ok(new ApiResponse<InvoiceDto>(true, "OK", invoice));
    }

    private static async Task<IResult> CreateInvoice(CreateInvoiceRequest req, ISender sender, CancellationToken ct = default)
    {
        var created = await sender.Send(new CreateInvoiceCommand(req.BookingId, req.CompanyName, req.TotalAmount, req.Currency, req.Address, req.CityName, req.TaxCode, req.Receiver, req.ReceiverPhone, req.ReceiverEmail), ct);
        return Results.Created($"/api/admin/invoices/{created.Id}", new ApiResponse<InvoiceDto>(true, "Created", created));
    }

    private static async Task<IResult> UpdateInvoice(int id, CreateInvoiceRequest req, ISender sender, CancellationToken ct = default)
    {
        var updated = await sender.Send(new UpdateInvoiceCommand(id, req.CompanyName, req.Address, req.CityName, req.TaxCode, req.Receiver, req.ReceiverPhone, req.ReceiverEmail, req.TotalAmount, req.Currency), ct);
        return updated is null ? Results.NotFound(new ApiResponse<object>(false, "Invoice not found", null)) : Results.Ok(new ApiResponse<InvoiceDto>(true, "Updated", updated));
    }

    private static async Task<IResult> DeleteInvoice(int id, ISender sender, CancellationToken ct = default)
    {
        var success = await sender.Send(new DeleteInvoiceCommand(id), ct);
        return success ? Results.NoContent() : Results.NotFound(new ApiResponse<object>(false, "Invoice not found", null));
    }
}