using FluentValidation.Results;

namespace Shared.BuildingBlocks.Exceptions;

public sealed class FlightValidationException(IEnumerable<ValidationFailure> failures)
    : BaseException("One or more validation errors occurred.", 400)
{
    public IEnumerable<ValidationFailure> Failures { get; } = failures;
}
