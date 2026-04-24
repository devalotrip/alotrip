namespace Flight.Application.Dtos;

public sealed class SearchAnalyticDto
{
    public int Id { get; init; }
    public string? AgentCode { get; init; }
    public DateTime Time { get; init; }
    public string? StartPoint { get; init; }
    public string? EndPoint { get; init; }
    public int Itinerary { get; init; }
    public DateTime DepartDate { get; init; }
    public DateTime? ReturnDate { get; init; }
    public bool FlightType { get; init; }
    public string? IPAddress { get; init; }
}

public sealed class CreateSearchAnalyticRequest
{
    public string? AgentCode { get; set; }
    public string? StartPoint { get; set; }
    public string? EndPoint { get; set; }
    public int Itinerary { get; set; }
    public DateTime DepartDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public bool FlightType { get; set; }
    public string? IPAddress { get; set; }
}
