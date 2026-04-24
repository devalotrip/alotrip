namespace Shared.BuildingBlocks.Exceptions;

public sealed class UnauthorizedException(string message)
    : BaseException(message, statusCode: 401);
