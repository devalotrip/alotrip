using Flight.Application.Dtos;
using Flight.Application.Interfaces;
using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Enums;
using Flight.Domain.Exceptions;
using Flight.Domain.Repositories;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks.Abstractions;
using Shared.BuildingBlocks.CQRS;

namespace Flight.Application.Features.BookFlight;

// ── Command ──────────────────────────────────────────────────────────────────
public sealed record BookFlightCommand(BookFlightRequest Request) : ICommand<Guid>;

// ── Validator ────────────────────────────────────────────────────────────────
public sealed class BookFlightCommandValidator : AbstractValidator<BookFlightCommand>
{
    public BookFlightCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.FareId).NotEmpty().WithMessage("FareId is required.");
        RuleFor(x => x.Request.SessionData).NotEmpty().WithMessage("SessionData is required.");
        RuleFor(x => x.Request.AgentCode).NotEmpty().WithMessage("AgentCode is required.");
        RuleFor(x => x.Request.ContactEmail).NotEmpty().EmailAddress().WithMessage("Valid contact email is required.");
        RuleFor(x => x.Request.ContactPhone).NotEmpty().WithMessage("Contact phone is required.");
        RuleFor(x => x.Request.Passengers).NotEmpty().WithMessage("At least one passenger is required.");

        RuleForEach(x => x.Request.Passengers).ChildRules(p =>
        {
            p.RuleFor(x => x.FirstName).NotEmpty();
            p.RuleFor(x => x.LastName).NotEmpty();
            p.RuleFor(x => x.Gender).Must(g => g is "M" or "F").WithMessage("Gender must be M or F.");
        });
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────
public sealed class BookFlightCommandHandler(
    IEnumerable<IFlightEngine> engines,
    IFlightCacheService cache,
    IBookingRepository bookingRepository,
    IBookingAddonRepository addonRepository,
    IUnitOfWork unitOfWork,
    IConfiguration configuration,
    ILogger<BookFlightCommandHandler> logger)
    : ICommandHandler<BookFlightCommand, Guid>
{
    public async Task<Guid> Handle(BookFlightCommand command, CancellationToken ct)
    {
        var req = command.Request;

        // 1. Lấy FareData từ cache
        var fareData = await cache.GetFareDataAsync(req.FareId, ct)
            ?? throw new FareExpiredException(req.FareId);

        // 2. Guard: chặn đặt trùng
        // Tier 1 — quick check: same fareId + agentCode (same search, double-click)
        bool isDuplicate = await bookingRepository.ExistsActiveByFareIdAsync(req.FareId, req.AgentCode, ct);
        if (isDuplicate)
            throw new DuplicateBookingException(req.FareId, req.AgentCode);

        // Tier 2 — deep check: same route/segments/passengers/contact within time window (cross-search)
        await CheckDeepDuplicateAsync(req, fareData, ct);

        // 3. Tìm engine tương ứng
        var engine = engines.FirstOrDefault(e => e.Source == fareData.Source && e.IsEnabled)
            ?? throw new DomainException($"Engine '{fareData.Source}' is not available.");

        // 4. Verify fare trước khi book.
        //    Một số engines (Datacom, Maybay, Kiwi) không có verify step riêng — trả null
        //    có nghĩa là "price will be confirmed at booking time", KHÔNG phải "fare expired".
        //    Chỉ throw FareExpiredException nếu engine CÓ verify step nhưng trả null (Galileo, Pkfare).
        bool engineHasVerify = fareData.Source is FlightSource.Galileo or FlightSource.Pkfare;
        var verifiedFare = await engine.VerifyFareAsync(req.FareId, req.SessionData, ct);

        if (verifiedFare == null)
        {
            if (engineHasVerify)
                throw new FareExpiredException(req.FareId);

            // Engine không có verify step → dùng cached fare làm verified
            verifiedFare = fareData;
            logger.LogDebug("Engine {Source} has no verify step — using cached fare for booking.", engine.Source);
        }

        if (Math.Abs(verifiedFare.TotalFare - fareData.TotalFare) > 1)
            throw new FarePriceChangedException(req.FareId, fareData.TotalFare, verifiedFare.TotalFare);

        // 5. Gọi engine để đặt chỗ
        logger.LogInformation("Booking via engine {Source} for agent {AgentCode}",
            engine.Source, req.AgentCode);

        var bookResult = await engine.BookFlightAsync(req, verifiedFare, ct);
        if (!bookResult.IsSuccess)
            throw new DomainException($"Booking failed: {bookResult.ErrorMessage}");

        // 6. Tạo BookingEntity (Aggregate Root)
        var booking = BookingEntity.Create(
            bookingCode : bookResult.BookingCode,
            agentCode   : req.AgentCode,
            source      : fareData.Source,
            tripType    : fareData.TripType,
            origin      : fareData.Origin,
            destination : fareData.Destination,
            departDate  : fareData.DepartDate,
            returnDate  : fareData.ReturnDate,
            totalAmount : verifiedFare.TotalFare,
            currency    : fareData.Currency,
            serviceFee  : verifiedFare.ServiceFee,
            contactName : req.ContactName,
            contactEmail: req.ContactEmail,
            contactPhone: req.ContactPhone,
            sessionId   : req.SessionData,
            fareId      : req.FareId,
            expiresAt   : bookResult.ExpiresAt);

        // 7. Thêm flights + passengers vào aggregate
        // Outbound segments
        foreach (var seg in fareData.OutboundSegments)
        {
            var flight = BookingFlightEntity.Create(
                booking.Id, seg.Origin, seg.Destination,
                seg.DepartTime, seg.ArriveTime, seg.Airline, seg.StopCount);

            // Thêm individual segments vào flight entity
            var segEntity = BookingSegmentEntity.Create(
                flight.Id, seg.FlightNumber, seg.Airline,
                seg.Origin, seg.Destination,
                seg.DepartTime, seg.ArriveTime,
                seg.CabinClass, seg.AircraftType);
            flight.AddSegment(segEntity);

            booking.AddFlight(flight);
        }

        // Return segments (round-trip)
        foreach (var seg in fareData.ReturnSegments)
        {
            var flight = BookingFlightEntity.Create(
                booking.Id, seg.Origin, seg.Destination,
                seg.DepartTime, seg.ArriveTime, seg.Airline, seg.StopCount);

            var segEntity = BookingSegmentEntity.Create(
                flight.Id, seg.FlightNumber, seg.Airline,
                seg.Origin, seg.Destination,
                seg.DepartTime, seg.ArriveTime,
                seg.CabinClass, seg.AircraftType);
            flight.AddSegment(segEntity);

            booking.AddFlight(flight);
        }

        foreach (var pax in req.Passengers)
        {
            var fareAmount = pax.Type switch
            {
                PassengerType.Adult  => verifiedFare.AdultFare,
                PassengerType.Child  => verifiedFare.ChildFare,
                PassengerType.Infant => verifiedFare.InfantFare,
                _                    => verifiedFare.AdultFare
            };

            var passenger = PassengerEntity.Create(
                booking.Id, pax.FirstName, pax.LastName, pax.Gender,
                pax.Type, fareAmount, fareData.Currency,
                pax.MiddleName, pax.BirthDate, pax.PassportNo,
                pax.PassportExpiry, pax.Nationality, pax.BaggageKg);

            booking.AddPassenger(passenger);
        }

        booking.Confirm();

        // 8. Lưu vào DB
        await bookingRepository.AddAsync(booking, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // 9. Lưu add-ons (Invoice, CarRental, TripTour, TripVisa, TripCancellation)
        //    Saved AFTER booking to get the bookingId, matching old SaveInvoice/SaveCarRentals/... flow.
        await SaveBookingAddOnsAsync(booking.Id, req, fareData.Currency, verifiedFare.TotalFare, ct);

        logger.LogInformation("Booking {BookingCode} created successfully with ID {BookingId}",
            bookResult.BookingCode, booking.Id);

        return booking.Id;
    }

    /// <summary>
    /// Saves optional add-ons attached to the booking request.
    /// Matches old Interface.cs: SaveInvoice → SaveCarRentals → SaveTripTours → SaveTripVisas → SaveTripCancel.
    /// Each add-on is saved best-effort — failure does not roll back the booking.
    /// </summary>
    private async Task SaveBookingAddOnsAsync(
        Guid bookingId, BookFlightRequest req, string currency, decimal totalFare, CancellationToken ct)
    {
        try
        {
            // Invoice
            if (req.Invoice is not null)
            {
                var invoice = InvoiceEntity.Create(
                    bookingId, req.Invoice.CompanyName, totalFare, currency,
                    req.Invoice.Address, req.Invoice.CityName, req.Invoice.TaxCode,
                    req.Invoice.Receiver, req.Invoice.ReceiverPhone, req.Invoice.ReceiverEmail);
                await addonRepository.AddInvoiceAsync(invoice, ct);
            }

            // Car Rentals
            if (req.CarRentals is { Count: > 0 })
            {
                foreach (var cr in req.CarRentals)
                {
                    int totalDays = Math.Max(1, (int)(cr.DropoffDate - cr.PickupDate).TotalDays);
                    var entity = CarRentalEntity.Create(
                        bookingId, cr.Provider, cr.PickupLocation, cr.DropoffLocation,
                        cr.PickupDate, cr.DropoffDate, cr.CarType, cr.CarModel,
                        cr.DailyRate, totalDays, cr.DailyRate * totalDays, cr.Currency);
                    await addonRepository.AddCarRentalAsync(entity, ct);
                }
            }

            // Trip Tours
            if (req.TripTours is { Count: > 0 })
            {
                foreach (var tt in req.TripTours)
                {
                    var entity = TripTourEntity.Create(
                        bookingId, tt.Name, tt.Code,
                        DateTime.UtcNow, DateTime.UtcNow, // DepartureDate/ReturnDate not in old DTO — default
                        string.Empty, 1, tt.Price, tt.Price, tt.Currency);
                    await addonRepository.AddTripTourAsync(entity, ct);
                }
            }

            // Trip Visas
            if (req.TripVisas is { Count: > 0 })
            {
                foreach (var tv in req.TripVisas)
                {
                    var entity = TripVisaEntity.Create(
                        bookingId, tv.Code, tv.Name, tv.Price, tv.Value, tv.Currency, tv.Price);
                    await addonRepository.AddTripVisaAsync(entity, ct);
                }
            }

            // Trip Cancellation
            if (req.TripCancellation is not null)
            {
                var tc = req.TripCancellation;
                var entity = TripCancellationEntity.Create(
                    bookingId, tc.MarkupAmount, tc.MarkupPercent,
                    tc.Price, tc.Currency, tc.BookingPrice, tc.Value);
                await addonRepository.AddTripCancellationAsync(entity, ct);
            }
        }
        catch (Exception ex)
        {
            // Best-effort: add-on save failure does not roll back the booking
            // This matches old code's try/catch per add-on
            logger.LogWarning(ex, "Failed to save one or more booking add-ons for booking {BookingId}", bookingId);
        }
    }

    /// <summary>
    /// Deep duplicate detection — matches old Interface.cs CheckDuplicateBooking logic.
    /// Finds recent bookings with the same route/date/agent, then compares:
    /// 1) Flight segments (origin, destination, departTime, arriveTime, airline, flightNumber)
    /// 2) Contact info (name, email, phone)
    /// 3) Passengers (type, firstName, lastName, gender, birthDate)
    /// Throws <see cref="DuplicateBookingException"/> if a full match is found.
    /// </summary>
    private async Task CheckDeepDuplicateAsync(
        BookFlightRequest req, FareDataDto fareData, CancellationToken ct)
    {
        var duplicateMinutes = configuration.GetValue<double>("DuplicateTimeMinutes", 30);
        if (duplicateMinutes <= 0) return; // disabled

        int passengerCount = req.Passengers.Count;

        var candidates = await bookingRepository.GetCandidateDuplicateBookingsAsync(
            fareData.Origin, fareData.Destination, fareData.DepartDate, fareData.ReturnDate,
            passengerCount, req.AgentCode, duplicateMinutes, ct);

        if (candidates.Count == 0) return;

        // Build the new booking's segment list and passenger list for comparison
        var newSegments = fareData.OutboundSegments
            .Concat(fareData.ReturnSegments)
            .OrderBy(s => s.DepartTime)
            .ToList();

        var newPassengers = req.Passengers
            .OrderBy(p => p.Type)
            .ThenBy(p => p.BirthDate)
            .ThenBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToList();

        foreach (var existing in candidates)
        {
            // ── 1. Compare flight segments ──────────────────────────────────
            var existingSegments = existing.Flights
                .SelectMany(f => f.Segments)
                .OrderBy(s => s.DepartTime)
                .ToList();

            if (existingSegments.Count != newSegments.Count)
                continue;

            bool segmentsDifferent = false;
            for (int i = 0; i < newSegments.Count; i++)
            {
                var ns = newSegments[i];
                var es = existingSegments[i];

                if (!string.Equals(es.Origin, ns.Origin, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(es.Destination, ns.Destination, StringComparison.OrdinalIgnoreCase) ||
                    es.DepartTime != ns.DepartTime ||
                    es.ArriveTime != ns.ArriveTime ||
                    !string.Equals(es.Airline, ns.Airline, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(es.FlightNumber, ns.FlightNumber, StringComparison.OrdinalIgnoreCase))
                {
                    segmentsDifferent = true;
                    break;
                }
            }
            if (segmentsDifferent) continue;

            // ── 2. Compare contact info ─────────────────────────────────────
            if (!string.Equals(existing.ContactName?.Trim(), req.ContactName?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(existing.ContactEmail?.Trim(), req.ContactEmail?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(existing.ContactPhone?.Trim(), req.ContactPhone?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // ── 3. Compare passengers ───────────────────────────────────────
            var existingPax = existing.Passengers
                .OrderBy(p => p.Type)
                .ThenBy(p => p.BirthDate)
                .ThenBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToList();

            if (existingPax.Count != newPassengers.Count)
                continue;

            bool passengersDifferent = false;
            for (int i = 0; i < newPassengers.Count; i++)
            {
                var np = newPassengers[i];
                var ep = existingPax[i];

                // Map PassengerType enum — old code compared string codes, new uses enum
                if (ep.Type != np.Type) { passengersDifferent = true; break; }

                if (!string.Equals(ep.FirstName?.Trim(), np.FirstName?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(ep.LastName?.Trim(), np.LastName?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    passengersDifferent = true;
                    break;
                }

                // Gender: stored as "M"/"F" string
                if (!string.Equals(ep.Gender, np.Gender, StringComparison.OrdinalIgnoreCase))
                {
                    passengersDifferent = true;
                    break;
                }

                // BirthDate: compare dates if both present; if both null, consider equal
                if (ep.BirthDate != np.BirthDate)
                {
                    passengersDifferent = true;
                    break;
                }
            }
            if (passengersDifferent) continue;

            // ── All checks passed — this is a duplicate ─────────────────────
            logger.LogWarning(
                "Deep duplicate detected: existing booking {BookingCode} (ID {BookingId}) matches new request for agent {AgentCode} on route {Origin}-{Destination}",
                existing.BookingCode, existing.Id, req.AgentCode, fareData.Origin, fareData.Destination);

            throw new DuplicateBookingException(
                existing.BookingCode, req.AgentCode, fareData.Origin, fareData.Destination);
        }
    }
}
