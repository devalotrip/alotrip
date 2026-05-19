using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using MediatR;
using System.Net.Http.Json;

namespace Flight.Application.Features.Admin.Currencies;

public sealed record GetCurrenciesQuery : IRequest<IEnumerable<CurrencyDto>>;
public sealed record UpdateCurrencyCommand(string Code, string? Name, decimal? Rate, string? Symbol, decimal? RoundUnit, bool? Locked, bool? Active) : IRequest<bool>;
public sealed record CreateCurrencyCommand(string Code, string? Name, decimal Rate, string? Symbol, decimal RoundUnit, bool Locked, bool Active) : IRequest<bool>;
public sealed record SyncCurrenciesCommand(string SecretKey) : IRequest<CurrencySyncResult>;

public sealed record OpenExchangeResponse(Dictionary<string, decimal> Rates);

public sealed class GetCurrenciesHandler(ICurrencyRepository repo) : IRequestHandler<GetCurrenciesQuery, IEnumerable<CurrencyDto>>
{
    public async Task<IEnumerable<CurrencyDto>> Handle(GetCurrenciesQuery request, CancellationToken ct)
    {
        return await repo.GetCurrenciesAsync(ct);
    }
}

public sealed class UpdateCurrencyHandler(ICurrencyRepository repo) : IRequestHandler<UpdateCurrencyCommand, bool>
{
    public async Task<bool> Handle(UpdateCurrencyCommand request, CancellationToken ct)
        => await repo.UpdateCurrencyAsync(request.Code, request.Name, request.Rate, request.Symbol, request.RoundUnit, request.Locked, request.Active, ct);
}

public sealed class CreateCurrencyHandler(ICurrencyRepository repo) : IRequestHandler<CreateCurrencyCommand, bool>
{
    public async Task<bool> Handle(CreateCurrencyCommand request, CancellationToken ct)
        => await repo.CreateCurrencyAsync(request.Code, request.Name, request.Rate, request.Symbol, request.RoundUnit, request.Locked, request.Active, ct);
}

public sealed class SyncCurrenciesHandler(ICurrencyRepository repo) : IRequestHandler<SyncCurrenciesCommand, CurrencySyncResult>
{
    private static readonly HttpClient _httpClient = new();
    private const string ExpectedKey = "F@Due8*MkQBdd&G";
    private const string ApiUrl = "https://openexchangerates.org/api/latest.json?app_id=5b3f001435bd4ceaabe0ece2892c730b";

    public async Task<CurrencySyncResult> Handle(SyncCurrenciesCommand request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.SecretKey) || request.SecretKey != ExpectedKey)
            return new CurrencySyncResult { Success = false, Message = "Unauthorized" };

        try
        {
            var response = await _httpClient.GetFromJsonAsync<OpenExchangeResponse>(ApiUrl, ct);
            if (response?.Rates == null)
                return new CurrencySyncResult { Success = false, Message = "Invalid API response" };

            var vndRate = response.Rates.GetValueOrDefault("VND", 0m);
            return await repo.SyncCurrenciesAsync(response.Rates, vndRate, ct);
        }
        catch (Exception ex)
        {
            return new CurrencySyncResult { Success = false, Message = $"Sync failed: {ex.Message}" };
        }
    }
}
