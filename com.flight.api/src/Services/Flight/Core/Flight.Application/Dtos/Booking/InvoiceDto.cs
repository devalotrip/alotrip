namespace Flight.Application.Dtos;

public sealed class InvoiceDto
{
    public int Id { get; init; }
    public Guid BookingId { get; init; }
    public string CompanyName { get; init; } = default!;
    public string? Address { get; init; }
    public string? CityName { get; init; }
    public string? TaxCode { get; init; }
    public string? Receiver { get; init; }
    public string? ReceiverPhone { get; init; }
    public string? ReceiverEmail { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "VND";
    public DateTime InvoiceDate { get; init; }
    public string? InvoiceNumber { get; init; }
}

public sealed class CreateInvoiceRequest
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
}