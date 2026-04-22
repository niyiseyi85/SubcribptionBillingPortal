using MediatR;
using Microsoft.Extensions.Logging;
using SubscriptionBillingPortal.Application.Contracts.Services;
using SubscriptionBillingPortal.Domain.Common;

namespace SubscriptionBillingPortal.Infrastructure.Services;

/// <summary>
/// Wraps a domain event in an INotification wrapper and dispatches it via MediatR.
/// This keeps the Domain layer free of any MediatR dependency.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IPublisher publisher, ILogger<DomainEventDispatcher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            _logger.LogInformation(
                "Dispatching domain event '{EventType}' with EventId '{EventId}'",
                domainEvent.GetType().Name,
                domainEvent.EventId);

            var notification = new DomainEventNotification(domainEvent);
            await _publisher.Publish(notification, cancellationToken);
        }
    }
}

/// <summary>
/// MediatR notification wrapper for domain events.
/// Allows domain events (which don't reference MediatR) to be published via the MediatR pipeline.
/// </summary>
public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;
