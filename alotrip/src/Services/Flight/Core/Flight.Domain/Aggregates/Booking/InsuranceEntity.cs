using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Booking;

public sealed class InsuranceEntity : Entity<int>
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
    public string Status { get; set; } = "Active";

    private InsuranceEntity() { }

    public static InsuranceEntity Create(
        Guid bookingId, Guid passengerId, string provider, string policyNumber,
        string coverageType, decimal premium, string currency, DateTime startDate, DateTime endDate,
        string? beneficiaryName = null, string? beneficiaryPhone = null)
    {
        return new InsuranceEntity
        {
            BookingId = bookingId,
            PassengerId = passengerId,
            Provider = provider,
            PolicyNumber = policyNumber,
            CoverageType = coverageType,
            Premium = premium,
            Currency = currency,
            StartDate = startDate,
            EndDate = endDate,
            BeneficiaryName = beneficiaryName,
            BeneficiaryPhone = beneficiaryPhone,
            CreatedOnUtc = DateTime.UtcNow
        };
    }
}