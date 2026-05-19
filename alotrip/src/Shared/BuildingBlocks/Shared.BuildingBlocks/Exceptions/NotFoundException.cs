namespace Shared.BuildingBlocks.Exceptions;

public sealed class NotFoundException(string message)
    : BaseException(message, statusCode: 404);
