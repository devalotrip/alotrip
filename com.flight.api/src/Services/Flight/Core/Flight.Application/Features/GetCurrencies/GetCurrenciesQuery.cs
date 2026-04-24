using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using MediatR;

namespace Flight.Application.Features.GetCurrencies;

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// GET /api/currencies — danh sách tất cả tiền tệ đang active.
/// </summary>
public sealed record GetCurrenciesQuery : IRequest<IEnumerable<CurrencyDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetCurrenciesHandler(ICurrencyRepository repo)
    : IRequestHandler<GetCurrenciesQuery, IEnumerable<CurrencyDto>>
{
    public Task<IEnumerable<CurrencyDto>> Handle(GetCurrenciesQuery request, CancellationToken ct)
        => repo.GetAllActiveAsync(ct);
}
