using Shared.BuildingBlocks.Exceptions;

namespace Flight.Domain.Exceptions;

public sealed class DomainException(string message)
    : BaseException(message, 422) { }

public sealed class BookingNotFoundException(Guid bookingId)
    : BaseException($"Booking with ID '{bookingId}' was not found.", 404) { }

public sealed class FareExpiredException(string fareId)
    : BaseException($"Fare '{fareId}' has expired. Please search again.", 410) { }

public sealed class FarePriceChangedException(string fareId, decimal oldPrice, decimal newPrice)
    : BaseException($"Fare '{fareId}' price changed from {oldPrice:N0} to {newPrice:N0}.", 409)
{
    public decimal OldPrice { get; } = oldPrice;
    public decimal NewPrice { get; } = newPrice;
}

public sealed class BookingAlreadyTicketedException(Guid bookingId)
    : BaseException($"Booking '{bookingId}' has already been ticketed.", 409) { }

public sealed class DuplicateBookingException : BaseException
{
    public string? ExistingBookingCode { get; }

    /// <summary>Quick duplicate: same fareId + agentCode.</summary>
    public DuplicateBookingException(string fareId, string agentCode)
        : base($"An active booking for fare '{fareId}' already exists for agent '{agentCode}'."
             + " Cancel the existing booking before creating a new one.", 409) { }

    /// <summary>Deep duplicate: same route/segments/passengers/contact within time window.</summary>
    public DuplicateBookingException(string existingBookingCode, string agentCode, string origin, string destination)
        : base($"Duplicate booking detected for agent '{agentCode}' on route {origin}-{destination}."
             + $" Existing booking: {existingBookingCode}.", 409)
    {
        ExistingBookingCode = existingBookingCode;
    }
}
