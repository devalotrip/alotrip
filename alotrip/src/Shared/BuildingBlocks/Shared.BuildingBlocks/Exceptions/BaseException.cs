namespace Shared.BuildingBlocks.Exceptions;

public abstract class BaseException(string message, int statusCode = 500)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
