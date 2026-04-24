namespace Flight.Application.Dtos;

/// <summary>
/// Min fare cache entry per route/date.
/// Matches old tblCache fields from the legacy system.
/// Populated as a side-effect of every flight search (passive cache).
/// </summary>
public sealed class MinFareEntryDto
{
    public string   Origin       { get; set; } = default!;
    public string   Destination  { get; set; } = default!;
    public DateTime DepartDate   { get; set; }
    public string?  Airline      { get; set; }
    public decimal  MinPrice     { get; set; }
    public decimal  ServiceFee   { get; set; }
    public string   Currency     { get; set; } = default!;

    /// <summary>1 = one-way, 2 = round-trip (matches old ItineraryType).</summary>
    public int       ItineraryType { get; set; }
    public DateTime? ReturnDate    { get; set; }

    /// <summary>UTC timestamp of when this entry was cached.</summary>
    public DateTime  SearchedAt    { get; set; }
}
