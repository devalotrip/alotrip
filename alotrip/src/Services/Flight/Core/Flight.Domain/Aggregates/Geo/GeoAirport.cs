using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Geo;

public sealed class GeoAirport : Entity<string>
{
    public string CityCode { get; set; } = default!;
    public string NameVi { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string NameFr { get; set; } = default!;
    public string? Location { get; set; }
    public string? SearchKeys { get; set; }
    public bool Visible { get; set; } = true;

    public GeoCity? City { get; set; }

    private GeoAirport() { }

    public static GeoAirport Create(string code, string cityCode, string nameVi, string nameEn, string nameFr, bool visible = true, string? location = null, string? searchKeys = null)
    {
        return new GeoAirport
        {
            Id = code,
            CityCode = cityCode,
            NameVi = nameVi,
            NameEn = nameEn,
            NameFr = nameFr,
            Location = location,
            SearchKeys = searchKeys,
            Visible = visible
        };
    }
}