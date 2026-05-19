using Flight.Application.Dtos;

namespace Flight.Application.Interfaces;

/// <summary>
/// Applies agent-specific commission and service fees to a list of search results.
/// Implementation lives in Infrastructure — queries <c>commissions</c> table.
/// </summary>
public interface ICommissionService
{
    /// <summary>
    /// For each fare in <paramref name="fares"/>, looks up the matching
    /// commission row for <paramref name="agentCode"/> (based on airline group
    /// and origin/destination region) and mutates BaseFare, ServiceFee, TotalFare.
    /// Returns the same list (mutated in-place) for fluent chaining.
    /// If <paramref name="agentCode"/> is null / not found, fares are returned unchanged.
    /// </summary>
    Task<IList<FareDataDto>> ApplyAsync(
        IList<FareDataDto> fares,
        string?            agentCode,
        CancellationToken  ct = default);
}
