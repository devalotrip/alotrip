using Flight.Domain.Aggregates.Booking;

namespace Flight.Domain.Repositories;

public interface ISearchAnalyticRepository
{
    Task SaveChangesAsync(CancellationToken ct = default);

    IQueryable<SearchAnalyticEntity> GetSearchAnalyticsQuery();
    Task<SearchAnalyticEntity?> GetSearchAnalyticByIdAsync(int id, CancellationToken ct = default);
    Task AddSearchAnalyticAsync(SearchAnalyticEntity entity, CancellationToken ct = default);
}
