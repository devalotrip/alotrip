using Flight.Domain.Exceptions;

namespace Flight.Domain.ValueObjects;

/// <summary>
/// Đại diện số tiền + đơn vị tiền tệ. Map từ tblCurrency + MoneyExchange cũ.
/// </summary>
public sealed record Money
{
    public decimal Amount   { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount   = amount;
        Currency = currency;
    }

    public static Money Of(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO code (e.g. VND, USD).");

        return new Money(amount, currency.ToUpperInvariant());
    }

    public static Money Zero(string currency) => Of(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot add {Currency} and {other.Currency}.");
        return Of(Amount + other.Amount, Currency);
    }

    public override string ToString() => $"{Amount:N0} {Currency}";
}
