using Flight.Application.Dtos;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;

namespace Flight.Application.Features.Admin.SearchAnalytics;

public sealed record GetSearchAnalyticsQuery(
    string? AgentCode = null, DateTime? FromDate = null, DateTime? ToDate = null,
    int Page = 1, int PageSize = 20) : IRequest<SearchAnalyticsResponse>;

public sealed record GetSearchAnalyticByIdQuery(int Id) : IRequest<SearchAnalyticDto?>;

public sealed record CreateSearchAnalyticCommand(
    string? AgentCode, string? StartPoint, string? EndPoint,
    int Itinerary, DateTime DepartDate, DateTime? ReturnDate,
    bool FlightType, string? IPAddress) : IRequest<SearchAnalyticDto>;

public sealed record SearchAnalyticsResponse(
    int Total, int Page, int PageSize, IEnumerable<SearchAnalyticDto> Data);

// ── List (with optional filters) ─────────────────────────────────────────────
public sealed class GetSearchAnalyticsHandler(ISearchAnalyticRepository repo)
    : IRequestHandler<GetSearchAnalyticsQuery, SearchAnalyticsResponse>
{
    public async Task<SearchAnalyticsResponse> Handle(GetSearchAnalyticsQuery request, CancellationToken ct)
    {
        var query = repo.GetSearchAnalyticsQuery().Where(s => s.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(request.AgentCode))
            query = query.Where(s => s.AgentCode == request.AgentCode);
        if (request.FromDate.HasValue)
            query = query.Where(s => s.Time >= request.FromDate);
        if (request.ToDate.HasValue)
            query = query.Where(s => s.Time <= request.ToDate);

        var total = await Task.Run(() => query.Count(), ct);
        var list = query
            .OrderByDescending(s => s.Time)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new SearchAnalyticsResponse(total, request.Page, request.PageSize, list.Select(e => MapDto(e)));
    }

    private static SearchAnalyticDto MapDto(SearchAnalyticEntity e) => new()
    {
        Id = e.Id, AgentCode = e.AgentCode, Time = e.Time, StartPoint = e.StartPoint,
        EndPoint = e.EndPoint, Itinerary = e.Itinerary, DepartDate = e.DepartDate,
        ReturnDate = e.ReturnDate, FlightType = e.FlightType, IPAddress = e.IPAddress
    };
}

// ── Get by Id ────────────────────────────────────────────────────────────────
public sealed class GetSearchAnalyticByIdHandler(ISearchAnalyticRepository repo)
    : IRequestHandler<GetSearchAnalyticByIdQuery, SearchAnalyticDto?>
{
    public async Task<SearchAnalyticDto?> Handle(GetSearchAnalyticByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetSearchAnalyticByIdAsync(request.Id, ct);
        return entity is null ? null : new SearchAnalyticDto
        {
            Id = entity.Id, AgentCode = entity.AgentCode, Time = entity.Time, StartPoint = entity.StartPoint,
            EndPoint = entity.EndPoint, Itinerary = entity.Itinerary, DepartDate = entity.DepartDate,
            ReturnDate = entity.ReturnDate, FlightType = entity.FlightType, IPAddress = entity.IPAddress
        };
    }
}

// ── Create (admin manual insert) ─────────────────────────────────────────────
public sealed class CreateSearchAnalyticHandler(ISearchAnalyticRepository repo)
    : IRequestHandler<CreateSearchAnalyticCommand, SearchAnalyticDto>
{
    public async Task<SearchAnalyticDto> Handle(CreateSearchAnalyticCommand request, CancellationToken ct)
    {
        var entity = SearchAnalyticEntity.Create(
            request.AgentCode, request.StartPoint, request.EndPoint,
            request.Itinerary, request.DepartDate, request.ReturnDate,
            request.FlightType, request.IPAddress);
        await repo.AddSearchAnalyticAsync(entity, ct);
        return new SearchAnalyticDto
        {
            Id = entity.Id, AgentCode = entity.AgentCode, Time = entity.Time, StartPoint = entity.StartPoint,
            EndPoint = entity.EndPoint, Itinerary = entity.Itinerary, DepartDate = entity.DepartDate,
            ReturnDate = entity.ReturnDate, FlightType = entity.FlightType, IPAddress = entity.IPAddress
        };
    }
}
