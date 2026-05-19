using Flight.Domain.Repositories;
using MediatR;

namespace Flight.Application.Features.Admin.Analytics;

public sealed record GetBookingsSummaryQuery(
    string? AgentCode = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<BookingsSummaryDto>;

public sealed class GetBookingsSummaryHandler(IAdminBookingRepository repo) : IRequestHandler<GetBookingsSummaryQuery, BookingsSummaryDto>
{
    public async Task<BookingsSummaryDto> Handle(GetBookingsSummaryQuery request, CancellationToken ct)
        => await repo.GetBookingsSummaryAsync(request.AgentCode, request.FromDate, request.ToDate, ct);
}

public sealed record GetTicketsIssuedQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<TicketsIssuedDto>;

public sealed class GetTicketsIssuedHandler(IAdminBookingRepository repo) : IRequestHandler<GetTicketsIssuedQuery, TicketsIssuedDto>
{
    public async Task<TicketsIssuedDto> Handle(GetTicketsIssuedQuery request, CancellationToken ct)
        => await repo.GetTicketsIssuedAsync(request.FromDate, request.ToDate, ct);
}

public sealed record GetSearchDetailsQuery(
    string? AgentCode = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<SearchDetailsDto>;

public sealed class GetSearchDetailsHandler(IAdminBookingRepository repo) : IRequestHandler<GetSearchDetailsQuery, SearchDetailsDto>
{
    public async Task<SearchDetailsDto> Handle(GetSearchDetailsQuery request, CancellationToken ct)
        => await repo.GetSearchDetailsAsync(request.AgentCode, request.FromDate, request.ToDate, request.Page, request.PageSize, ct);
}
