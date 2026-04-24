using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Geo;

public sealed class GeoCity : Entity<string>
{
    public string CountryCode { get; set; } = default!;
    public string NameVi { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string NameFr { get; set; } = default!;
    public string? Location { get; set; }
    public string? SearchKeys { get; set; }
    public bool Visible { get; set; } = true;

    public GeoCountry? Country { get; set; }

    private readonly List<GeoAirport> _airports = [];
    public IReadOnlyList<GeoAirport> Airports => _airports.AsReadOnly();

    private GeoCity() { }

    public static GeoCity Create(string code, string countryCode, string nameVi, string nameEn, string nameFr, bool visible = true, string? location = null, string? searchKeys = null)
    {
        return new GeoCity
        {
            Id = code,
            CountryCode = countryCode,
            NameVi = nameVi,
            NameEn = nameEn,
            NameFr = nameFr,
            Location = location,
            SearchKeys = searchKeys,
            Visible = visible
        };
    }
}