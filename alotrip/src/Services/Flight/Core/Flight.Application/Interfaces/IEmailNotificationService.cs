namespace Flight.Application.Interfaces;

/// <summary>
/// Gửi email thông báo booking. Map từ NotifyBooking.cs + mailHelper.cs cũ.
/// </summary>
public interface IEmailNotificationService
{
    Task SendBookingConfirmationAsync(string toEmail, string toName,
        string bookingCode, string origin, string destination,
        DateTime departDate, decimal totalAmount, string currency,
        CancellationToken ct = default);

    Task SendTicketIssuedAsync(string toEmail, string toName,
        string bookingCode, string ticketNumber,
        CancellationToken ct = default);

    Task SendBookingCancelledAsync(string toEmail, string toName,
        string bookingCode, string reason,
        CancellationToken ct = default);
}
