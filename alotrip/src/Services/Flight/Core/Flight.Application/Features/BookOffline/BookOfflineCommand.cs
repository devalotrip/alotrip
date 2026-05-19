using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Enums;
using Flight.Domain.Exceptions;
using Flight.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks.Abstractions;
using Shared.BuildingBlocks.CQRS;

namespace Flight.Application.Features.BookOffline;

/// <summary>
/// Re-submits an existing booking (Pending/Failed) to the airline engine.
/// Map from old AirlineWS.BookOffline(PayWithAgencyCredit, BookingId):
///   1. Load existing booking + passengers from DB
///   2. Reconstruct fare data / request from stored booking info
///   3. Call engine.BookFlightAsync to create airline reservation
///   4. Update BookingCode + Status on existing record
///   No new booking records are created.
/// </summary>
public sealed record BookOfflineCommand(
    Guid BookingId,
    bool PayWithAgencyCredit = false
) : ICommand<BookResultDto>;

public sealed class BookOfflineCommandHandler(
    IEnumerable<IFlightEngine> engines,
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    ILogger<BookOfflineCommandHandler> logger)
    : ICommandHandler<BookOfflineCommand, BookResultDto>
{
    public async Task<BookResultDto> Handle(BookOfflineCommand command, CancellationToken ct)
    {
        // 1. Load booking with all navigation (flights + segments + passengers)
        var booking = await bookingRepository.GetByIdWithDetailsAsync(command.BookingId, ct)
            ?? throw new DomainException($"Booking {command.BookingId} not found.");

        // 2. Validate state — only Pending or Failed bookings can be re-submitted
        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Failed)
            throw new DomainException(
                $"Cannot book offline: booking is in status '{booking.Status}'. " +
                "Only Pending or Failed bookings can be re-submitted.");

        // 3. Find the engine
        var engine = engines.FirstOrDefault(e => e.Source == booking.Source && e.IsEnabled)
            ?? throw new DomainException($"Engine '{booking.Source}' is not available for offline booking.");

        // 4. Reconstruct FareDataDto from booking data
        var fareData = ReconstructFareData(booking);

        // 5. Reconstruct BookFlightRequest from booking data
        var request = ReconstructBookRequest(booking, fareData, command.PayWithAgencyCredit);

        // 6. Submit to engine
        logger.LogInformation(
            "BookOffline: re-submitting booking {BookingId} via engine {Source} for agent {AgentCode}",
            booking.Id, engine.Source, booking.AgentCode);

        BookResultDto bookResult;
        try
        {
            bookResult = await engine.BookFlightAsync(request, fareData, ct);
        }
        catch (Exception ex)
        {
            // Engine failed — mark booking as Failed
            booking.MarkFailed();
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogWarning(ex,
                "BookOffline: engine {Source} failed for booking {BookingId}",
                engine.Source, booking.Id);

            return new BookResultDto
            {
                IsSuccess = false,
                ErrorMessage = $"Engine booking failed: {ex.Message}"
            };
        }

        // 7. Update existing booking based on result
        if (bookResult.IsSuccess && !string.IsNullOrEmpty(bookResult.BookingCode))
        {
            booking.ConfirmOffline(bookResult.BookingCode);
            logger.LogInformation(
                "BookOffline: booking {BookingId} confirmed with code {BookingCode}",
                booking.Id, bookResult.BookingCode);
        }
        else
        {
            booking.MarkFailed();
            logger.LogWarning(
                "BookOffline: booking {BookingId} failed — {Error}",
                booking.Id, bookResult.ErrorMessage);
        }

        await unitOfWork.SaveChangesAsync(ct);

        return bookResult;
    }

    /// <summary>
    /// Reconstructs a FareDataDto from the stored booking entity.
    /// This allows the engine to book without a cached search result.
    /// Note: SelectedValue on segments will be null — engines that require it
    /// (Datacom, Maybay) may need fresh search data or handle offline differently.
    /// </summary>
    private static FareDataDto ReconstructFareData(BookingEntity booking)
    {
        // Separate flights into outbound and return based on origin matching
        var flights = booking.Flights.OrderBy(f => f.DepartTime).ToList();

        var outboundFlights = new List<BookingFlightEntity>();
        var returnFlights = new List<BookingFlightEntity>();

        if (booking.TripType == TripType.RoundTrip && flights.Count >= 2)
        {
            // Flights departing from booking.Origin are outbound,
            // flights departing from booking.Destination are return
            foreach (var f in flights)
            {
                if (f.Origin.Equals(booking.Destination, StringComparison.OrdinalIgnoreCase))
                    returnFlights.Add(f);
                else
                    outboundFlights.Add(f);
            }
        }
        else
        {
            outboundFlights.AddRange(flights);
        }

        // Map segments
        var outboundSegments = outboundFlights
            .SelectMany(f => f.Segments.Select(s => new FlightSegmentDto
            {
                FlightNumber = s.FlightNumber,
                Airline      = s.Airline,
                Origin       = s.Origin,
                Destination  = s.Destination,
                DepartTime   = s.DepartTime,
                ArriveTime   = s.ArriveTime,
                CabinClass   = s.CabinClass,
                AircraftType = s.AircraftType,
                StopCount    = 0,
                SelectedValue = null // Not stored — engines needing this must handle offline specially
            }))
            .OrderBy(s => s.DepartTime)
            .ToList();

        var returnSegments = returnFlights
            .SelectMany(f => f.Segments.Select(s => new FlightSegmentDto
            {
                FlightNumber = s.FlightNumber,
                Airline      = s.Airline,
                Origin       = s.Origin,
                Destination  = s.Destination,
                DepartTime   = s.DepartTime,
                ArriveTime   = s.ArriveTime,
                CabinClass   = s.CabinClass,
                AircraftType = s.AircraftType,
                StopCount    = 0,
                SelectedValue = null
            }))
            .OrderBy(s => s.DepartTime)
            .ToList();

        // Extract per-type fare from passengers
        var passengers = booking.Passengers.ToList();
        decimal adultFare  = passengers.FirstOrDefault(p => p.Type == PassengerType.Adult)?.FareAmount ?? 0;
        decimal childFare  = passengers.FirstOrDefault(p => p.Type == PassengerType.Child)?.FareAmount ?? 0;
        decimal infantFare = passengers.FirstOrDefault(p => p.Type == PassengerType.Infant)?.FareAmount ?? 0;

        int adultCount  = passengers.Count(p => p.Type == PassengerType.Adult);
        int childCount  = passengers.Count(p => p.Type == PassengerType.Child);
        int infantCount = passengers.Count(p => p.Type == PassengerType.Infant);

        string airline = outboundSegments.FirstOrDefault()?.Airline
                      ?? flights.FirstOrDefault()?.Airline
                      ?? string.Empty;

        return new FareDataDto
        {
            FareId      = booking.FareId ?? $"offline-{booking.Id}",
            Source      = booking.Source,
            Airline     = airline,
            Origin      = booking.Origin,
            Destination = booking.Destination,
            DepartDate  = booking.DepartDate,
            ReturnDate  = booking.ReturnDate,
            TripType    = booking.TripType,
            AdultCount  = adultCount,
            ChildCount  = childCount,
            InfantCount = infantCount,
            AdultFare   = adultFare,
            ChildFare   = childFare,
            InfantFare  = infantFare,
            TaxAmount   = 0, // Tax not stored separately — included in TotalAmount
            ServiceFee  = booking.ServiceFee,
            TotalFare   = booking.TotalAmount,
            Currency    = booking.Currency,
            SessionData = booking.SessionId,
            PccCode     = booking.PccCode,
            OutboundSegments = outboundSegments,
            ReturnSegments   = returnSegments,
            CachedAt  = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
    }

    private static BookFlightRequest ReconstructBookRequest(
        BookingEntity booking,
        FareDataDto fareData,
        bool payWithAgencyCredit)
    {
        var passengers = booking.Passengers
            .Select(p => new PassengerBookingDto
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
                BaggageKg      = p.BaggageKg
            })
            .ToList();

        return new BookFlightRequest
        {
            FareId       = fareData.FareId,
            SessionData  = fareData.SessionData ?? string.Empty,
            AgentCode    = booking.AgentCode,
            ContactName  = booking.ContactName,
            ContactEmail = booking.ContactEmail,
            ContactPhone = booking.ContactPhone,
            Passengers   = passengers
        };
    }
}
