using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Shared.BuildingBlocks.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest  : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[START] Handle request={Request} - Response={Response} - RequestData={@RequestData}",
            typeof(TRequest).Name, typeof(TResponse).Name, request);

        var timer = Stopwatch.StartNew();
        var response = await next();
        timer.Stop();

        if (timer.Elapsed.TotalSeconds > 3)
            logger.LogWarning(
                "[PERFORMANCE] Request {Request} took {Elapsed}s — exceeds 3s threshold.",
                typeof(TRequest).Name, timer.Elapsed.TotalSeconds);

        logger.LogInformation(
            "[END] Handled {Request} with {Response}",
            typeof(TRequest).Name, typeof(TResponse).Name);

        return response;
    }
}
