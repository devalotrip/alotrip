using Carter;
using Flight.Application.Dtos;
using Flight.Application.Features.BookFlight;
using Flight.Application.Features.BookOffline;
using Flight.Application.Features.GetBaggagesByBooking;
using Flight.Application.Features.GetBookingByCode;
using Flight.Application.Features.GetBookingById;
using Flight.Application.Features.GetBookings;
using Flight.Application.Features.GetTickets;
using Flight.Application.Features.IssueTicket;
using Flight.Application.Features.RebookFlight;
using MediatR;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Shared.BuildingBlocks.Authentication.Extensions;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Booking;

/// <summary>
/// Booking endpoints.
/// Map từ AirlineWS.BookWithDetail() + ApiWS.IssueTicket() SOAP → REST:
///   POST   /api/bookings                       — đặt chỗ
///   POST   /api/bookings/{id}/tickets          — phát hành vé
///   POST   /api/bookings/{id}/rebook           — đặt lại từ booking cũ (hết hạn/huỷ)
///   GET    /api/bookings                       — danh sách bookings của agent (paginated)
///   GET    /api/bookings/{id:guid}             — chi tiết booking theo ID
///   GET    /api/bookings/by-code/{code}        — chi tiết booking theo booking code (PNR)
/// </summary>
public sealed class BookingEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/bookings", HandleBookAsync)
            .WithName("BookFlight")
            .WithTags("Bookings")
            .WithSummary("Đặt chỗ chuyến bay")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiCreatedResponse<Guid>>(201)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(401)
            .Produces<ProblemDetails>(409);

        app.MapPost("/api/bookings/{bookingId:guid}/tickets", HandleIssueTicketAsync)
            .WithName("IssueTicket")
            .WithTags("Bookings")
            .WithSummary("Phát hành vé điện tử")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(401);

        // Get baggages for a booking (currently placeholder until engine integrations added)
        app.MapGet("/api/bookings/{bookingId:guid}/baggages", HandleGetBaggagesAsync)
            .WithName("GetBaggagesForBooking")
            .WithTags("Bookings")
            .WithSummary("Lấy thông tin baggages cho booking")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<BaggageInfoDto>>(200)
            .Produces<ProblemDetails>(401);

        app.MapPost("/api/bookings/{bookingId:guid}/rebook", HandleRebookAsync)
            .WithName("RebookFlight")
            .WithTags("Bookings")
            .WithSummary("Đặt lại chuyến bay từ booking cũ đã hết hạn hoặc bị huỷ")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<RebookResultDto>>(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(401)
            .Produces<ProblemDetails>(404)
            .Produces<ProblemDetails>(422);

        app.MapPost("/api/bookings/{bookingId:guid}/book-offline", HandleBookOfflineAsync)
            .WithName("BookOffline")
            .WithTags("Bookings")
            .WithSummary("Gửi lại booking Pending/Failed cho hãng bay (offline retry)")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<BookResultDto>>(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(401)
            .Produces<ProblemDetails>(404);

        app.MapGet("/api/bookings", HandleGetBookingsAsync)
            .WithName("GetBookings")
            .WithTags("Bookings")
            .WithSummary("Danh sách bookings của agent (paginated)")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200)
            .Produces<ProblemDetails>(401);

        // Note: route with literal segment must come BEFORE /{id:guid} to avoid ambiguity
        app.MapGet("/api/bookings/by-code/{code}", HandleGetBookingByCodeAsync)
            .WithName("GetBookingByCode")
            .WithTags("Bookings")
            .WithSummary("Chi tiết booking theo booking code (PNR)")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200)
            .Produces<ProblemDetails>(401)
            .Produces<ProblemDetails>(404);

        app.MapGet("/api/bookings/{id:guid}", HandleGetBookingByIdAsync)
            .WithName("GetBookingById")
            .WithTags("Bookings")
            .WithSummary("Chi tiết booking theo ID")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200)
            .Produces<ProblemDetails>(401)
            .Produces<ProblemDetails>(404);

        // Get tickets for a booking
        app.MapGet("/api/bookings/{bookingId:guid}/tickets", HandleGetTicketsByBookingIdAsync)
            .WithName("GetTicketsByBookingId")
            .WithTags("Bookings")
            .WithSummary("Lấy danh sách vé của một booking")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200)
            .Produces<ProblemDetails>(401);

        app.MapGet("/api/tickets/{ticketNumber}", HandleGetTicketByNumberAsync)
            .WithName("GetTicketByNumber")
            .WithTags("Tickets")
            .WithSummary("Lookup ticket by ticket number")
            .RequireAuthorization("AgentOnly")
            .Produces<ApiResponse<object>>(200);
    }

    private static async Task<IResult> HandleBookAsync(
        ISender sender,
        IHttpContextAccessor httpContextAccessor,
        BookFlightRequest request,
        CancellationToken ct = default)
    {
        // AgentCode phải lấy từ JWT — client không được tự set
        request.AgentCode = httpContextAccessor.GetCurrentUser().UserName;

        var command = new BookFlightCommand(request);
        var bookingId = await sender.Send(command, ct);
        return Results.Created($"/api/bookings/{bookingId}", new ApiCreatedResponse<Guid>(bookingId));
    }

    private static async Task<IResult> HandleIssueTicketAsync(
        ISender sender,
        IHttpContextAccessor httpContextAccessor,
        Guid bookingId,
        CancellationToken ct = default)
    {
        var agentCode = httpContextAccessor.GetCurrentUser().UserName;

        var command = new IssueTicketCommand(bookingId, agentCode);
        var result = await sender.Send(command, ct);
        return Results.Ok(new ApiResponse<object>(true, "Tickets issued successfully.", result));
    }

    private static async Task<IResult> HandleRebookAsync(
        ISender sender,
        IHttpContextAccessor httpContextAccessor,
        Guid bookingId,
        CancellationToken ct = default)
    {
        var agentCode = httpContextAccessor.GetCurrentUser().UserName;
        var command = new RebookFlightCommand(bookingId, agentCode);
        var result = await sender.Send(command, ct);
        return Results.Ok(new ApiResponse<RebookResultDto>(true, "Rebook successful.", result));
    }

    private static async Task<IResult> HandleGetBookingsAsync(
        ISender sender,
        IHttpContextAccessor httpContextAccessor,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var agentCode = httpContextAccessor.GetCurrentUser().UserName;
        var query = new GetBookingsQuery(agentCode, page, pageSize);
        var result = await sender.Send(query, ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    private static async Task<IResult> HandleGetBookingByIdAsync(
        ISender sender,
        Guid id,
        CancellationToken ct = default)
    {
        var query = new GetBookingByIdQuery(id);
        var result = await sender.Send(query, ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    private static async Task<IResult> HandleGetBookingByCodeAsync(
        ISender sender,
        string code,
        CancellationToken ct = default)
    {
        var query = new GetBookingByCodeQuery(code);
        var result = await sender.Send(query, ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    private static async Task<IResult> HandleGetTicketsByBookingIdAsync(
        ISender sender,
        Guid bookingId,
        CancellationToken ct = default)
    {
        var query = new global::Flight.Application.Features.GetTickets.GetTicketsByBookingIdQuery(bookingId);
        var result = await sender.Send(query, ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    private static async Task<IResult> HandleGetTicketByNumberAsync(
        ISender sender,
        string ticketNumber,
        CancellationToken ct = default)
    {
        // Use MediatR query to fetch ticket by number
        var query = new global::Flight.Application.Features.GetTicketByNumber.GetTicketByNumberQuery(ticketNumber);
        var result = await sender.Send(query, ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    private static async Task<IResult> HandleGetBaggagesAsync(
        ISender sender,
        Guid bookingId,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetBaggagesByBookingQuery(bookingId), ct);
        return Results.Ok(new ApiResponse<BaggageInfoDto>(true, "OK", result));
    }

    private static async Task<IResult> HandleBookOfflineAsync(
        ISender sender,
        Guid bookingId,
        bool payWithAgencyCredit = false,
        CancellationToken ct = default)
    {
        var command = new BookOfflineCommand(bookingId, payWithAgencyCredit);
        var result = await sender.Send(command, ct);
        return Results.Ok(new ApiResponse<BookResultDto>(true,
            result.IsSuccess ? "Booking submitted successfully." : $"Booking failed: {result.ErrorMessage}",
            result));
    }
}
