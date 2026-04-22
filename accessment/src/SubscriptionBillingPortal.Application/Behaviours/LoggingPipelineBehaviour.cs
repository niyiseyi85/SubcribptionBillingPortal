using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SubscriptionBillingPortal.Application.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that logs every request's start, successful completion,
/// and elapsed time. Provides consistent structured observability across all handlers.
/// </summary>
public sealed class LoggingPipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingPipelineBehaviour<TRequest, TResponse>> _logger;

    public LoggingPipelineBehaviour(ILogger<LoggingPipelineBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Processing request '{RequestName}' {@Request}",
            requestName,
            request);

        try
        {
            var response = await next(cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Request '{RequestName}' completed successfully in {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Request '{RequestName}' failed after {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
