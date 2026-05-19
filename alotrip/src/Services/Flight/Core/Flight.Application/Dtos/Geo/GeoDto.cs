namespace Flight.Application.Dtos;

public sealed class GeoContinentDto
{
    public string Code { get; init; } = default!;
    public string NameVi { get; init; } = default!;
    public string NameEn { get; init; } = default!;
    public string NameFr { get; init; } = default!;
    public bool Visible { get; init; } = true;
}

public sealed class GeoCountryDto
{
    public string Code { get; init; } = default!;
    public string ContinentCode { get; init; } = default!;
    public string NameVi { get; init; } = default!;
    public string NameEn { get; init; } = default!;
    public string NameFr { get; init; } = default!;
    public string? Flag { get; init; }
    public bool Visible { get; init; } = true;
}

public sealed class GeoCityDto
{
    public string Code { get; init; } = default!;
    public string CountryCode { get; init; } = default!;
    public string NameVi { get; init; } = default!;
    public string NameEn { get; init; } = default!;
    public string NameFr { get; init; } = default!;
    public string? Location { get; init; }
    public string? SearchKeys { get; init; }
    public bool Visible { get; init; } = true;
}

public sealed class GeoAirportDto
{
    public string Code { get; init; } = default!;
    public string CityCode { get; init; } = default!;
    public string NameVi { get; init; } = default!;
    public string NameEn { get; init; } = default!;
    public string NameFr { get; init; } = default!;
    public string? Location { get; init; }
    public string? SearchKeys { get; init; }
    public bool Visible { get; init; } = true;
}