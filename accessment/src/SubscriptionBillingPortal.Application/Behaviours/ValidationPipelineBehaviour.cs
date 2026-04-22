using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SubscriptionBillingPortal.Application.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that validates all commands and queries using FluentValidation
/// before they reach their handlers. Throws a ValidationException on failure.
/// </summary>
public sealed class ValidationPipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationPipelineBehaviour<TRequest, TResponse>> _logger;

    public ValidationPipelineBehaviour(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationPipelineBehaviour<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
        {
            _logger.LogWarning(
                "Validation failed for '{RequestType}' with {FailureCount} error(s): {Errors}",
                typeof(TRequest).Name,
                failures.Count,
                string.Join(", ", failures.Select(f => f.ErrorMessage)));

            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
