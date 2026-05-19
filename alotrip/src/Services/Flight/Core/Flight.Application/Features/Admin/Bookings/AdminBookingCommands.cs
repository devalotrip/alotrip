using Flight.Domain.Aggregates.Booking;
using Flight.Domain.Repositories;
using MediatR;
using Shared.Common.Responses;

namespace Flight.Application.Features.Admin.Bookings;

public sealed record GetAdminBookingsQuery(
    string? AgentCode = null,
    string? Status = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 50) : IRequest<IEnumerable<AdminBookingListDto>>;

public sealed record GetAdminBookingByIdQuery(Guid Id) : IRequest<AdminBookingDetailDto?>;
public sealed record UpdateBookingStatusCommand(Guid Id, string Status) : IRequest<bool>;

public sealed class GetAdminBookingsHandler(IAdminBookingRepository repo) : IRequestHandler<GetAdminBookingsQuery, IEnumerable<AdminBookingListDto>>
{
    public async Task<IEnumerable<AdminBookingListDto>> Handle(GetAdminBookingsQuery request, CancellationToken ct)
    {
        var result = await repo.GetAdminBookingsAsync(request.AgentCode, request.Status, request.From, request.To, request.Page, request.PageSize, ct);
        return result.Select(b => new AdminBookingListDto
        {
            Id = b.Id, BookingCode = b.BookingCode, AgentCode = b.AgentCode,
            Source = b.Source, TripType = b.TripType, Status = b.Status,
            Origin = b.Origin, Destination = b.Destination, DepartDate = b.DepartDate, ReturnDate = b.ReturnDate,
            TotalAmount = b.TotalAmount, Currency = b.Currency,
            ContactName = b.ContactName, ContactEmail = b.ContactEmail,
            ExpiresAt = b.ExpiresAt, CreatedOnUtc = b.CreatedOnUtc,
            PassengerCount = b.PassengerCount, TicketCount = b.TicketCount
        });
    }
}

public sealed class GetAdminBookingByIdHandler(IAdminBookingRepository repo) : IRequestHandler<GetAdminBookingByIdQuery, AdminBookingDetailDto?>
{
    public async Task<AdminBookingDetailDto?> Handle(GetAdminBookingByIdQuery request, CancellationToken ct)
    {
        var result = await repo.GetAdminBookingByIdAsync(request.Id, ct);
        if (result is null) return null;
        return new AdminBookingDetailDto
        {
            Id = result.Id, BookingCode = result.BookingCode, AgentCode = result.AgentCode,
            Source = result.Source, TripType = result.TripType, Status = result.Status,
            Origin = result.Origin, Destination = result.Destination,
            DepartDate = result.DepartDate, ReturnDate = result.ReturnDate,
            TotalAmount = result.TotalAmount, ServiceFee = result.ServiceFee, Currency = result.Currency,
            ContactName = result.ContactName, ContactEmail = result.ContactEmail, ContactPhone = result.ContactPhone,
            ExpiresAt = result.ExpiresAt, CreatedOnUtc = result.CreatedOnUtc,
            FareId = result.FareId, PccCode = result.PccCode,
            Passengers = result.Passengers.Select(p => new AdminPassengerDto
            {
                FirstName = p.FirstName, LastName = p.LastName, Gender = p.Gender,
                Type = p.Type, BirthDate = p.BirthDate, PassportNo = p.PassportNo,
                BaggageKg = p.BaggageKg, FareAmount = p.FareAmount
            }).ToList(),
            Flights = result.Flights.Select(f => new AdminFlightDto
            {
                Airline = f.Airline, Origin = f.Origin, Destination = f.Destination,
                DepartTime = f.DepartTime, ArriveTime = f.ArriveTime,
                Segments = f.Segments.Select(s => new AdminSegmentDto
                {
                    FlightNumber = s.FlightNumber, Airline = s.Airline, Origin = s.Origin, Destination = s.Destination,
                    DepartTime = s.DepartTime, ArriveTime = s.ArriveTime, CabinClass = s.CabinClass, AircraftType = s.AircraftType
                }).ToList()
            }).ToList(),
            Tickets = result.Tickets.Select(t => new AdminTicketDto
            {
                TicketNumber = t.TicketNumber, PassengerName = t.PassengerName, Airline = t.Airline, IssuedAt = t.IssuedAt
            }).ToList()
        };
    }
}

public sealed class UpdateBookingStatusHandler(IAdminBookingRepository repo) : IRequestHandler<UpdateBookingStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateBookingStatusCommand request, CancellationToken ct)
        => await repo.UpdateBookingStatusAsync(request.Id, request.Status, ct);
}

public sealed class AdminBookingListDto
{
    public Guid Id { get; init; }
    public string BookingCode { get; init; } = default!;
    public string AgentCode { get; init; } = default!;
    public string Source { get; init; } = default!;
    public string TripType { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string Origin { get; init; } = default!;
    public string Destination { get; init; } = default!;
    public DateTime DepartDate { get; init; }
    public DateTime? ReturnDate { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = default!;
    public string ContactName { get; init; } = default!;
    public string ContactEmail { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public int PassengerCount { get; init; }
    public int TicketCount { get; init; }
}

public sealed class AdminBookingDetailDto
{
    public Guid Id { get; init; }
    public string BookingCode { get; init; } = default!;
    public string AgentCode { get; init; } = default!;
    public string Source { get; init; } = default!;
    public string TripType { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string Origin { get; init; } = default!;
    public string Destination { get; init; } = default!;
    public DateTime DepartDate { get; init; }
    public DateTime? ReturnDate { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal ServiceFee { get; init; }
    public string Currency { get; init; } = default!;
    public string ContactName { get; init; } = default!;
    public string ContactEmail { get; init; } = default!;
    public string? ContactPhone { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public string? FareId { get; init; }
    public string? PccCode { get; init; }
    public List<AdminPassengerDto> Passengers { get; init; } = [];
    public List<AdminFlightDto> Flights { get; init; } = [];
    public List<AdminTicketDto> Tickets { get; init; } = [];
}

public sealed class AdminPassengerDto
{
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string Gender { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string? BirthDate { get; init; }
    public string? PassportNo { get; init; }
    public decimal? BaggageKg { get; init; }
    public decimal FareAmount { get; init; }
}

public sealed class AdminFlightDto
{
    public string Airline { get; init; } = default!;
    public string Origin { get; init; } = default!;
    public string Destination { get; init; } = default!;
    public DateTime DepartTime { get; init; }
    public DateTime ArriveTime { get; init; }
    public List<AdminSegmentDto> Segments { get; init; } = [];
}

public sealed class AdminSegmentDto
{
    public string FlightNumber { get; init; } = default!;
    public string Airline { get; init; } = default!;
    public string Origin { get; init; } = default!;
    public string Destination { get; init; } = default!;
    public DateTime DepartTime { get; init; }
    public DateTime ArriveTime { get; init; }
    public string CabinClass { get; init; } = default!;
    public string AircraftType { get; init; } = default!;
}

public sealed class AdminTicketDto
{
    public string TicketNumber { get; init; } = default!;
    public string PassengerName { get; init; } = default!;
    public string Airline { get; init; } = default!;
    public DateTime IssuedAt { get; init; }
}