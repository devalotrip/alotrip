namespace Flight.Application.Dtos;

public sealed class CurrencyDto
{
    public string  Code      { get; init; } = default!;
    public string? Name      { get; init; }
    public decimal Rate      { get; init; }
    public string? Symbol    { get; init; }
    public decimal RoundUnit { get; init; }
    public bool    Locked    { get; init; }
    public bool    Active    { get; init; }
}
