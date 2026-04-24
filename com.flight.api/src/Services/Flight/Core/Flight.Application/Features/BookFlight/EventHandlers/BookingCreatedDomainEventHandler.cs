using Flight.Application.Interfaces;
using Flight.Domain.Aggregates.Booking.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Flight.Application.Features.BookFlight.EventHandlers;

public sealed class BookingCreatedDomainEventHandler(
    IEmailNotificationService emailService,
    ILogger<BookingCreatedDomainEventHandler> logger)
    : INotificationHandler<BookingCreatedDomainEvent>
{
    public async Task Handle(BookingCreatedDomainEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Sending booking confirmation for {BookingCode} to {Email}",
            notification.BookingCode, notification.ContactEmail);

        await emailService.SendBookingConfirmationAsync(
            notification.ContactEmail,
            notification.ContactEmail,
            notification.BookingCode,
            notification.Origin,
            notification.Destination,
            notification.CreatedAt.DateTime,
            notification.TotalAmount,
            notification.Currency,
            ct);
    }
}
