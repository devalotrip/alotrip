using Carter;
using Flight.Application.Features.Admin.Bookings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Responses;

namespace Flight.Api.Endpoints.Admin.Bookings;

public sealed class AdminBookingEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/admin/bookings")
            .RequireAuthorization("AdminOnly")
            .WithTags("Admin - Bookings");

        grp.MapGet("/", HandleListAsync)
            .WithName("AdminListBookings")
            .WithSummary("Danh sách tất cả bookings (tất cả agents, có filter)");

        grp.MapGet("/{id:guid}", HandleGetByIdAsync)
            .WithName("AdminGetBooking")
            .WithSummary("Chi tiết booking theo ID");

        grp.MapPatch("/{id:guid}/status", HandleUpdateStatusAsync)
            .WithName("AdminUpdateBookingStatus")
            .WithSummary("Cập nhật trạng thái booking (admin override)");
    }

    private static async Task<IResult> HandleListAsync(
        ISender sender,
        string? agentCode = null,
        string? status = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAdminBookingsQuery(agentCode, status, from, to, page, pageSize), ct);
        return Results.Ok(new ApiResponse<object>(true, "Success", result));
    }

    private static async Task<IResult> HandleGetByIdAsync(
        ISender sender,
        Guid id,
        CancellationToken ct = default)
    {
        var booking = await sender.Send(new GetAdminBookingByIdQuery(id), ct);
        return booking is null ? Results.NotFound() : Results.Ok(new ApiResponse<object>(true, "Success", booking));
    }

    private static async Task<IResult> HandleUpdateStatusAsync(
        ISender sender,
        Guid id,
        string newStatus,
        CancellationToken ct = default)
    {
        var success = await sender.Send(new UpdateBookingStatusCommand(id, newStatus), ct);
        return success
            ? Results.Ok(new ApiResponse<object>(true, $"Status updated to {newStatus}.", null))
            : Results.NotFound();
    }
}