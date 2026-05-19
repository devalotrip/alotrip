namespace Flight.Application.Dtos;

public sealed class ClassAndNoteDto
{
    public int Id { get; init; }
    public string AirlineCode { get; init; } = default!;
    public string Class { get; init; } = default!;
    public string? ShowClass { get; init; }
    public bool NonRefundable { get; init; }
    public bool Visible { get; init; }
    public string? StartAirportCode { get; init; }
    public string? EndAirportCode { get; init; }
}

public sealed class CreateClassAndNoteRequest
{
    public string AirlineCode { get; set; } = default!;
    public string Class { get; set; } = default!;
    public string? ShowClass { get; set; }
    public bool NonRefundable { get; set; }
    public string? StartAirportCode { get; set; }
    public string? EndAirportCode { get; set; }
}