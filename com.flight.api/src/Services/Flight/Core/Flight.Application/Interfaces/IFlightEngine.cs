using Flight.Application.Dtos;
using Flight.Domain.Enums;

namespace Flight.Application.Interfaces;

/// <summary>
/// Interface cho mỗi engine tìm kiếm chuyến bay (Galileo, Datacom, Kiwi, Pkfare, Maybay).
/// Clean Architecture: Application layer chỉ biết interface — Infrastructure implement.
/// </summary>
public interface IFlightEngine
{
    FlightSource Source { get; }
    bool IsEnabled      { get; }

    Task<IEnumerable<FareDataDto>> SearchFlightAsync(SearchFlightRequest request, CancellationToken ct = default);

    /// <summary>Re-validates the fare price immediately before booking.</summary>
    Task<FareDataDto?> VerifyFareAsync(string fareId, string sessionData, CancellationToken ct = default);

    /// <summary>Submits the booking with the engine.</summary>
    Task<BookResultDto> BookFlightAsync(BookFlightRequest request, FareDataDto fareData, CancellationToken ct = default);

    Task<IssueTicketResultDto> IssueTicketAsync(string bookingCode, string sessionData, CancellationToken ct = default);

    /// <summary>
    /// Lấy thông tin hành lý mua thêm cho chuyến bay.
    /// Receives the full FareDataDto (from cache) so each engine can access
    /// SessionData, segments (SelectedValue/FlightValue), airline, etc.
    /// Trả về empty BaggageInfoDto nếu engine không hỗ trợ.
    /// </summary>
    Task<BaggageInfoDto> GetBaggagesAsync(FareDataDto fareData, CancellationToken ct = default);

    /// <summary>
    /// Lấy điều kiện vé (hoàn, đổi, quy định hành lý).
    /// Receives the full FareDataDto so Galileo can access stored RulesInfo XML.
    /// itinerary: 0 = outbound, 1 = return.
    /// Trả về empty list nếu engine không hỗ trợ.
    /// </summary>
    Task<List<FareRuleGroupDto>> GetFareRulesAsync(FareDataDto fareData, int itinerary, CancellationToken ct = default);
}
