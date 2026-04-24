using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Booking;

public sealed class InvoiceEntity : Entity<int>
{
    public Guid BookingId { get; set; }
    public string CompanyName { get; set; } = default!;
    public string? Address { get; set; }
    public string? CityName { get; set; }
    public string? TaxCode { get; set; }
    public string? Receiver { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? ReceiverEmail { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime InvoiceDate { get; set; }
    public string? InvoiceNumber { get; set; }

    private InvoiceEntity() { }

    public static InvoiceEntity Create(
        Guid bookingId, string companyName, decimal totalAmount,
        string currency = "VND", string? address = null, string? cityName = null,
        string? taxCode = null, string? receiver = null, string? receiverPhone = null,
        string? receiverEmail = null)
    {
        return new InvoiceEntity
        {
            BookingId = bookingId,
            CompanyName = companyName,
            Address = address,
            CityName = cityName,
            TaxCode = taxCode,
            Receiver = receiver,
            ReceiverPhone = receiverPhone,
            ReceiverEmail = receiverEmail,
            TotalAmount = totalAmount,
            Currency = currency,
            InvoiceDate = DateTime.UtcNow,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}"
        };
    }
}