using System.Net;
using System.Net.Mail;
using Flight.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Flight.Infrastructure.Services;

/// <summary>
/// Sends transactional email via SMTP.
/// Configuration keys (in appsettings.json under "Email"):
///   Host, Port, EnableSsl, Username, Password, FromAddress, FromName
/// </summary>
public sealed class SmtpEmailNotificationService(
    IConfiguration             config,
    ILogger<SmtpEmailNotificationService> logger)
    : IEmailNotificationService
{
    public async Task SendBookingConfirmationAsync(
        string toEmail, string toName, string bookingCode,
        string origin, string destination,
        DateTime departDate, decimal totalAmount, string currency,
        CancellationToken ct = default)
    {
        string subject = $"[Alotrip] Booking Confirmed – {bookingCode}";
        string body    = $"""
            Dear {toName},

            Your booking has been confirmed.

              Booking Code : {bookingCode}
              Route        : {origin} → {destination}
              Departure    : {departDate:dd MMM yyyy HH:mm}
              Total Amount : {totalAmount:N0} {currency}

            Please keep this code for check-in. Tickets will be issued shortly.

            Thank you for booking with Alotrip.
            """;

        await SendAsync(toEmail, toName, subject, body, ct);
    }

    public async Task SendTicketIssuedAsync(
        string toEmail, string toName, string bookingCode, string ticketNumber,
        CancellationToken ct = default)
    {
        string subject = $"[Alotrip] Ticket Issued – {ticketNumber}";
        string body    = $"""
            Dear {toName},

            Your e-ticket has been issued successfully.

              Booking Code  : {bookingCode}
              Ticket Number : {ticketNumber}

            Please present this ticket number at check-in.

            Thank you for booking with Alotrip.
            """;

        await SendAsync(toEmail, toName, subject, body, ct);
    }

    public async Task SendBookingCancelledAsync(
        string toEmail, string toName, string bookingCode, string reason,
        CancellationToken ct = default)
    {
        string subject = $"[Alotrip] Booking Cancelled – {bookingCode}";
        string body    = $"""
            Dear {toName},

            Your booking {bookingCode} has been cancelled.

            Reason: {reason}

            If you did not request this cancellation, please contact support immediately.

            Thank you,
            Alotrip Support
            """;

        await SendAsync(toEmail, toName, subject, body, ct);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private async Task SendAsync(
        string toEmail, string toName,
        string subject, string body,
        CancellationToken ct)
    {
        try
        {
            var sec = config.GetSection("Email");

            string host        = sec["Host"]        ?? "smtp.gmail.com";
            int    port        = int.Parse(sec["Port"]  ?? "587");
            bool   enableSsl   = bool.Parse(sec["EnableSsl"] ?? "true");
            string username    = sec["Username"]    ?? "";
            string password    = sec["Password"]    ?? "";
            string fromAddress = sec["FromAddress"] ?? username;
            string fromName    = sec["FromName"]    ?? "Alotrip";

            using var client = new SmtpClient(host, port)
            {
                EnableSsl   = enableSsl,
                Credentials = new NetworkCredential(username, password),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var message = new MailMessage
            {
                From    = new MailAddress(fromAddress, fromName),
                Subject = subject,
                Body    = body,
                IsBodyHtml = false
            };
            message.To.Add(new MailAddress(toEmail, toName));

            await client.SendMailAsync(message, ct);

            logger.LogInformation("[Email] Sent '{Subject}' to {Email}", subject, toEmail);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Email failures must not crash a booking flow
            logger.LogWarning(ex, "[Email] Failed to send '{Subject}' to {Email}", subject, toEmail);
        }
    }
}
