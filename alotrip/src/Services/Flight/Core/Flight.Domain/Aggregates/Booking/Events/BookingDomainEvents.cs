using Shared.BuildingBlocks.Abstractions;

namespace Flight.Domain.Aggregates.Booking.Events;

/// <summary>
/// Phát sinh khi booking được tạo thành công — dùng để gửi email xác nhận.
/// </summary>
public sealed record BookingCreatedDomainEvent(
    Guid   BookingId,
    string BookingCode,
    string AgentCode,
    string ContactEmail,
    string ContactPhone,
    string Origin,
    string Destination,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAt) : IDomainEvent;

/// <summary>
/// Phát sinh khi vé được phát hành thành công.
/// </summary>
public sealed record TicketIssuedDomainEvent(
    Guid   BookingId,
    string BookingCode,
    string TicketNumber,
    string PassengerName,
    string ContactEmail,
    DateTimeOffset IssuedAt) : IDomainEvent;

/// <summary>
/// Phát sinh khi booking bị hủy.
/// </summary>
public sealed record BookingCancelledDomainEvent(
    Guid   BookingId,
    string BookingCode,
    string Reason,
    DateTimeOffset CancelledAt) : IDomainEvent;
