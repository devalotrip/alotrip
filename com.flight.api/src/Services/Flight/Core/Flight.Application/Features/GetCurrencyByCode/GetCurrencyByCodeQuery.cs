using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using MediatR;
using Shared.BuildingBlocks.Exceptions;

namespace Flight.Application.Features.GetCurrencyByCode;

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// GET /api/currencies/{code} — thông tin tiền tệ theo mã (VND, USD, EUR, ...).
/// </summary>
public sealed record GetCurrencyByCodeQuery(string Code) : IRequest<CurrencyDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetCurrencyByCodeHandler(ICurrencyRepository repo)
    : IRequestHandler<GetCurrencyByCodeQuery, CurrencyDto>
{
    public async Task<CurrencyDto> Handle(GetCurrencyByCodeQuery request, CancellationToken ct)
    {
        var currency = await repo.GetByCodeAsync(request.Code.ToUpperInvariant(), ct)
            ?? throw new NotFoundException($"Currency '{request.Code}' not found.");
        return currency;
    }
}
