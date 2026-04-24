using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Exceptions;
using Flight.Domain.Repositories;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks.Abstractions;
using Shared.BuildingBlocks.CQRS;

namespace Flight.Application.Features.IssueTicket;

// ── Command ──────────────────────────────────────────────────────────────────
public sealed record IssueTicketCommand(Guid BookingId, string AgentCode)
    : ICommand<IssueTicketResultDto>;

// ── Validator ────────────────────────────────────────────────────────────────
public sealed class IssueTicketCommandValidator : AbstractValidator<IssueTicketCommand>
{
    public IssueTicketCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.AgentCode).NotEmpty();
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────
public sealed class IssueTicketCommandHandler(
    IEnumerable<IFlightEngine> engines,
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    ILogger<IssueTicketCommandHandler> logger)
    : ICommandHandler<IssueTicketCommand, IssueTicketResultDto>
{
    public async Task<IssueTicketResultDto> Handle(IssueTicketCommand command, CancellationToken ct)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(command.BookingId, ct)
            ?? throw new BookingNotFoundException(command.BookingId);

        var engine = engines.FirstOrDefault(e => e.Source == booking.Source && e.IsEnabled)
            ?? throw new DomainException($"Engine '{booking.Source}' is not available.");

        logger.LogInformation("Issuing ticket for booking {BookingCode} via {Source}",
            booking.BookingCode, booking.Source);

        var result = await engine.IssueTicketAsync(booking.BookingCode, booking.SessionId ?? "", ct);

        if (!result.IsSuccess)
            throw new DomainException($"Ticket issuance failed: {result.ErrorMessage}");

        // Gắn ticket vào aggregate cho từng hành khách.
        // Engine returns ticket numbers that must be matched to passengers.
        // Use ordered list (both sides sorted consistently) rather than positional index alone,
        // and guard against count mismatch.
        var passengers = booking.Passengers.OrderBy(p => p.Type).ThenBy(p => p.LastName).ThenBy(p => p.FirstName).ToList();

        for (int i = 0; i < result.TicketNumbers.Count && i < passengers.Count; i++)
        {
            var passenger = passengers[i];
            var ticket = TicketEntity.Create(
                booking.Id, passenger.Id,
                result.TicketNumbers[i],
                passenger.FullName,
                booking.Flights.FirstOrDefault()?.Airline ?? booking.Source.ToString());

            booking.IssueTicket(ticket);
        }

        if (result.TicketNumbers.Count != passengers.Count)
        {
            logger.LogWarning(
                "Ticket count ({TicketCount}) does not match passenger count ({PaxCount}) for booking {BookingCode}",
                result.TicketNumbers.Count, passengers.Count, booking.BookingCode);
        }

        bookingRepository.Update(booking);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Tickets issued for booking {BookingCode}: {Tickets}",
            booking.BookingCode, string.Join(", ", result.TicketNumbers));

        return result;
    }
}
