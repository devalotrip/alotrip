namespace Flight.Application.Dtos;

/// <summary>
/// DTO for passenger type lookup table (ADT/CHD/INF with multilingual names).
/// Matches old tblPassengerType.
/// </summary>
public sealed class PassengerTypeDto
{
    public string Code        { get; init; } = default!;
    public string? Icon       { get; init; }
    public string? NameVi     { get; init; }
    public string? NameEn     { get; init; }
    public string? NameFr     { get; init; }
    public string? Description { get; init; }
}

public sealed class CreatePassengerTypeRequest
{
    public string Code        { get; set; } = default!;
    public string? Icon       { get; set; }
    public string? NameVi     { get; set; }
    public string? NameEn     { get; set; }
    public string? NameFr     { get; set; }
    public string? Description { get; set; }
}

public sealed class UpdatePassengerTypeRequest
{
    public string? Icon       { get; set; }
    public string? NameVi     { get; set; }
    public string? NameEn     { get; set; }
    public string? NameFr     { get; set; }
    public string? Description { get; set; }
}
