using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Geo;

public sealed class GeoCountry : Entity<string>
{
    public string ContinentCode { get; set; } = default!;
    public string NameVi { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string NameFr { get; set; } = default!;
    public string? Flag { get; set; }
    public bool Visible { get; set; } = true;

    public GeoContinent? Continent { get; set; }

    private readonly List<GeoCity> _countries = [];
    public IReadOnlyList<GeoCity> Cities => _countries.AsReadOnly();

    private GeoCountry() { }

    public static GeoCountry Create(string code, string continentCode, string nameVi, string nameEn, string nameFr, bool visible = true, string? flag = null)
    {
        return new GeoCountry
        {
            Id = code,
            ContinentCode = continentCode,
            NameVi = nameVi,
            NameEn = nameEn,
            NameFr = nameFr,
            Flag = flag,
            Visible = visible
        };
    }
}