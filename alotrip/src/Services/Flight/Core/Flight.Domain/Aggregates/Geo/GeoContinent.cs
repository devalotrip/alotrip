using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Geo;

public sealed class GeoContinent : Entity<string>
{
    public string NameVi { get; set; } = default!;
    public string NameEn { get; set; } = default!;
    public string NameFr { get; set; } = default!;
    public bool Visible { get; set; } = true;

    private readonly List<GeoCountry> _countries = [];
    public IReadOnlyList<GeoCountry> Countries => _countries.AsReadOnly();

    private GeoContinent() { }

    public static GeoContinent Create(string code, string nameVi, string nameEn, string nameFr, bool visible = true)
    {
        return new GeoContinent
        {
            Id = code,
            NameVi = nameVi,
            NameEn = nameEn,
            NameFr = nameFr,
            Visible = visible
        };
    }
}