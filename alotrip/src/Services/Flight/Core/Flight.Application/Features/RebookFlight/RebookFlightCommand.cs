using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Enums;
using Flight.Domain.Exceptions;
using Flight.Domain.Repositories;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks.Abstractions;
using Shared.BuildingBlocks.CQRS;

namespace Flight.Application.Features.RebookFlight;

// ── Command ──────────────────────────────────────────────────────────────────
/// <summary>
/// Tạo booking mới từ một booking cũ đã hết hạn / bị huỷ, sử dụng lại thông tin
/// hành khách và tuyến bay, nhưng với giá vé mới nhất từ cache.
/// </summary>
public sealed record RebookFlightCommand(Guid OldBookingId, string AgentCode)
    : ICommand<RebookResultDto>;

// ── Validator ─────────────────────────────────────────────────────────────────
public sealed class RebookFlightCommandValidator : AbstractValidator<RebookFlightCommand>
{
    public RebookFlightCommandValidator()
    {
        RuleFor(x => x.OldBookingId).NotEmpty();
        RuleFor(x => x.AgentCode).NotEmpty();
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class RebookFlightCommandHandler(
    IEnumerable<IFlightEngine> engines,
    IBookingRepository         bookingRepository,
    IFlightCacheService        cache,
    IUnitOfWork                unitOfWork,
    ILogger<RebookFlightCommandHandler> logger)
    : ICommandHandler<RebookFlightCommand, RebookResultDto>
{
    public async Task<RebookResultDto> Handle(RebookFlightCommand command, CancellationToken ct)
    {
        // 1. Load the old booking
        var old = await bookingRepository.GetByIdAsync(command.OldBookingId, ct)
            ?? throw new BookingNotFoundException(command.OldBookingId);

        // 2. Authorisation: booking must belong to the requesting agent
        if (!old.AgentCode.Equals(command.AgentCode, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("You are not authorised to rebook this booking.");

        // 3. Only Cancelled or Expired bookings can be rebooked
        if (old.Status is not (BookingStatus.Cancelled or BookingStatus.Expired))
            throw new DomainException(
                $"Only Cancelled or Expired bookings can be rebooked. Current status: {old.Status}.");

        // 4. Reconstruct pax counts from old passengers
        int adultCount  = old.Passengers.Count(p => p.Type == PassengerType.Adult);
        int childCount  = old.Passengers.Count(p => p.Type == PassengerType.Child);
        int infantCount = old.Passengers.Count(p => p.Type == PassengerType.Infant);

        // 5. Look for fresh fares in the search cache
        var cacheKey = BuildSearchCacheKey(old, adultCount, childCount, infantCount);
        var cachedFares = await cache.GetSearchResultAsync(cacheKey, ct);

        if (cachedFares == null || !cachedFares.Any())
            throw new DomainException(
                "No cached fares found for this route. Please run a new search before rebooking.");

        // 6. Pick the cheapest fare from the same source engine if possible,
        //    otherwise fall back to the overall cheapest available fare.
        var freshFare = cachedFares
            .Where(f => f.Source == old.Source)
            .MinBy(f => f.TotalFare)
            ?? cachedFares.MinBy(f => f.TotalFare)!;

        var engine = engines.FirstOrDefault(e => e.Source == freshFare.Source && e.IsEnabled)
            ?? throw new DomainException($"Engine '{freshFare.Source}' is not available.");

        // 7. Re-submit the booking via the engine with the original passenger data
        var rebookRequest = BuildBookRequest(old, freshFare);
        var bookResult = await engine.BookFlightAsync(rebookRequest, freshFare, ct);

        if (!bookResult.IsSuccess)
            throw new DomainException($"Rebook failed: {bookResult.ErrorMessage}");

        // 8. Persist new booking aggregate
        var newBooking = BookingEntity.Create(
            bookingCode : bookResult.BookingCode,
            agentCode   : old.AgentCode,
            source      : freshFare.Source,
            tripType    : freshFare.TripType,
            origin      : freshFare.Origin,
            destination : freshFare.Destination,
            departDate  : freshFare.DepartDate,
            returnDate  : freshFare.ReturnDate,
            totalAmount : freshFare.TotalFare,
            currency    : freshFare.Currency,
            serviceFee  : freshFare.ServiceFee,
            contactName : old.ContactName,
            contactEmail: old.ContactEmail,
            contactPhone: old.ContactPhone,
            sessionId   : freshFare.SessionData,
            fareId      : freshFare.FareId,
            expiresAt   : bookResult.ExpiresAt);

        // Add outbound segments
        foreach (var seg in freshFare.OutboundSegments)
        {
            var flight = BookingFlightEntity.Create(
                newBooking.Id, seg.Origin, seg.Destination,
                seg.DepartTime, seg.ArriveTime, seg.Airline, seg.StopCount);
            flight.AddSegment(BookingSegmentEntity.Create(
                flight.Id, seg.FlightNumber, seg.Airline,
                seg.Origin, seg.Destination,
                seg.DepartTime, seg.ArriveTime,
                seg.CabinClass, seg.AircraftType));
            newBooking.AddFlight(flight);
        }

        // Add return segments (round-trip)
        foreach (var seg in freshFare.ReturnSegments)
        {
            var flight = BookingFlightEntity.Create(
                newBooking.Id, seg.Origin, seg.Destination,
                seg.DepartTime, seg.ArriveTime, seg.Airline, seg.StopCount);
            flight.AddSegment(BookingSegmentEntity.Create(
                flight.Id, seg.FlightNumber, seg.Airline,
                seg.Origin, seg.Destination,
                seg.DepartTime, seg.ArriveTime,
                seg.CabinClass, seg.AircraftType));
            newBooking.AddFlight(flight);
        }

        // Re-use original passengers with updated fare amount
        foreach (var pax in old.Passengers)
        {
            decimal fareAmount = pax.Type switch
            {
                PassengerType.Adult  => freshFare.AdultFare,
                PassengerType.Child  => freshFare.ChildFare,
                PassengerType.Infant => freshFare.InfantFare,
                _                    => freshFare.AdultFare,
            };

            newBooking.AddPassenger(PassengerEntity.Create(
                newBooking.Id,
                pax.FirstName, pax.LastName, pax.Gender,
                pax.Type, fareAmount, freshFare.Currency,
                pax.MiddleName, pax.BirthDate, pax.PassportNo,
                pax.PassportExpiry, pax.Nationality, pax.BaggageKg));
        }

        newBooking.Confirm();

        await bookingRepository.AddAsync(newBooking, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Rebook: old={OldCode} → new={NewCode} ({OldPrice}→{NewPrice} {Currency})",
            old.BookingCode, bookResult.BookingCode,
            old.TotalAmount, freshFare.TotalFare, freshFare.Currency);

        return new RebookResultDto
        {
            NewBookingId = newBooking.Id,
            OldPrice     = old.TotalAmount,
            NewPrice     = freshFare.TotalFare,
            Currency     = freshFare.Currency,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildSearchCacheKey(
        BookingEntity booking, int adultCount, int childCount, int infantCount)
    {
        var agent = booking.AgentCode.ToLowerInvariant();
        return $"search:{agent}:{booking.Origin}:{booking.Destination}" +
               $":{booking.DepartDate:yyyyMMdd}:{adultCount}:{childCount}:{infantCount}:{booking.Currency}";
    }

    private static BookFlightRequest BuildBookRequest(BookingEntity old, FareDataDto freshFare)
    {
        return new BookFlightRequest
        {
            FareId       = freshFare.FareId,
            SessionData  = freshFare.SessionData ?? string.Empty,
            AgentCode    = old.AgentCode,
            ContactName  = old.ContactName,
            ContactEmail = old.ContactEmail,
            ContactPhone = old.ContactPhone,
            Passengers   = old.Passengers.Select(p => new PassengerBookingDto
            {
                FirstName      = p.FirstName,
                LastName       = p.LastName,
                MiddleName     = p.MiddleName,
                Gender         = p.Gender,
                Type           = p.Type,
                BirthDate      = p.BirthDate,
                PassportNo     = p.PassportNo,
                PassportExpiry = p.PassportExpiry,
                Nationality    = p.Nationality,
                BaggageKg      = p.BaggageKg,
            }).ToList(),
        };
    }
}
