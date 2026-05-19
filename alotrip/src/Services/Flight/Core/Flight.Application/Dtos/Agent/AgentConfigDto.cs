namespace Flight.Application.Dtos;

/// <summary>
/// Configuration loaded from the agents / agent_partners / agent_airline_ignores tables.
/// Used by SearchFlightQuery to decide which engines to call and which airlines to exclude.
/// </summary>
public sealed class AgentConfigDto
{
    public string AgentCode { get; init; } = default!;

    // ── Engine flags (from agents table) ──────────────────────────────────────
    public bool GalileoActive        { get; init; }
    public bool LccVnActiveDomestic  { get; init; }   // LCC active for VN domestic routes
    public bool LccVnActiveGlobal    { get; init; }   // LCC active for international routes via VN

    // ── Partner flags (derived from agent_partners JOIN partners) ─────────────
    public bool KiwiActive   { get; init; }
    public bool PkfareActive { get; init; }
    public bool MaybayActive { get; init; }
    public bool DatacomActive { get; init; }

    // ── Per-partner route restrictions (IgnoredMode + ListStartPoint) ─────────
    /// <summary>
    /// Route eligibility rules per partner engine.
    /// Matches old CheckSearchFlight logic from Interface.cs.
    /// </summary>
    public IReadOnlyList<PartnerRouteRuleDto> PartnerRouteRules { get; init; } = [];

    // ── Airline ignore rules (from agent_airline_ignores) ─────────────────────
    public IReadOnlyList<AirlineIgnoreDto> AirlineIgnores { get; init; } = [];
}

/// <summary>
/// Per-partner route eligibility rule.
/// IgnoredMode=0: "all routes EXCEPT these" (blacklist).
/// IgnoredMode=1: "ONLY these routes" (whitelist).
/// Routes are "{origin}{destination}" pairs (e.g. "SGNHAN").
/// </summary>
public sealed class PartnerRouteRuleDto
{
    /// <summary>Partner name (lowercase): "kiwi", "pkfare", "maybay", "datacom".</summary>
    public string PartnerName { get; init; } = default!;

    /// <summary>0 = blacklist (all except), 1 = whitelist (only these).</summary>
    public int IgnoredMode { get; init; }

    /// <summary>Comma-separated route pairs, e.g. "SGNHAN,HANSGN,DADSGN".</summary>
    public List<string> Routes { get; init; } = [];
}

/// <summary>
/// A per-agent airline suppression rule.
/// </summary>
public sealed class AirlineIgnoreDto
{
    /// <summary>IATA airline code, e.g. "VN", "VJ", "QH".</summary>
    public string AirlineCode { get; init; } = default!;

    /// <summary>Suppress fare when the plating carrier matches this code.</summary>
    public bool FilterByPlating { get; init; }

    /// <summary>Suppress fare when ANY segment is operated by this airline.</summary>
    public bool FilterByAnySegment { get; init; }

    /// <summary>Suppress fare only when ALL segments are operated by this airline.</summary>
    public bool FilterByAllSegments { get; init; }
}
