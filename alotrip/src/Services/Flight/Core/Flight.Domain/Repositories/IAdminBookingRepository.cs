namespace Flight.Domain.Repositories;

public interface IAdminBookingRepository
{
    Task<IEnumerable<AdminBookingListDto>> GetAdminBookingsAsync(string? agentCode, string? status, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default);
    Task<AdminBookingDetailDto?> GetAdminBookingByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> UpdateBookingStatusAsync(Guid id, string status, CancellationToken ct = default);

    // Analytics
    Task<BookingsSummaryDto> GetBookingsSummaryAsync(string? agentCode, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);
    Task<TicketsIssuedDto> GetTicketsIssuedAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);
    Task<SearchDetailsDto> GetSearchDetailsAsync(string? agentCode, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken ct = default);
}
