using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Shared.BuildingBlocks.Exceptions.Handler;

public sealed class CustomExceptionHandler(ILogger<CustomExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            FlightValidationException => (StatusCodes.Status400BadRequest,    exception.Message),
            BadHttpRequestException   => (StatusCodes.Status400BadRequest,    exception.Message),
            UnauthorizedException     => (StatusCodes.Status401Unauthorized,  exception.Message),
            NotFoundException         => (StatusCodes.Status404NotFound,      exception.Message),
            KeyNotFoundException      => (StatusCodes.Status404NotFound,      exception.Message),
            _                        => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Error: {Message} at {Time}", exception.Message, DateTime.UtcNow);
        else
            logger.LogWarning("Warning: {Message} at {Time}", exception.Message, DateTime.UtcNow);

        var details = new ProblemDetails
        {
            Status = statusCode,
            Title  = title,
            Detail = exception.Message,
        };

        if (exception is FlightValidationException validationEx)
            details.Extensions["errors"] = validationEx.Failures
                .Select(f => new { f.PropertyName, f.ErrorMessage });

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(details, cancellationToken);
        return true;
    }
}
