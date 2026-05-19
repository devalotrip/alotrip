namespace Flight.Application.Dtos;

public sealed class InsuranceDto
{
    public int Id { get; init; }
    public Guid BookingId { get; init; }
    public Guid PassengerId { get; init; }
    public string Provider { get; init; } = default!;
    public string PolicyNumber { get; init; } = default!;
    public string CoverageType { get; init; } = default!;
    public decimal Premium { get; init; }
    public string Currency { get; init; } = "VND";
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? BeneficiaryName { get; init; }
    public string? BeneficiaryPhone { get; init; }
    public string Status { get; init; } = "Active";
}

public sealed class CreateInsuranceRequest
{
    public Guid BookingId { get; set; }
    public Guid PassengerId { get; set; }
    public string Provider { get; set; } = default!;
    public string PolicyNumber { get; set; } = default!;
    public string CoverageType { get; set; } = default!;
    public decimal Premium { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? BeneficiaryName { get; set; }
    public string? BeneficiaryPhone { get; set; }
}