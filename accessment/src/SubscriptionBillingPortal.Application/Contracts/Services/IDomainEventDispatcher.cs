using SubscriptionBillingPortal.Domain.Common;

namespace SubscriptionBillingPortal.Application.Contracts.Services;

/// <summary>
/// Dispatches domain events collected from aggregates after persistence is complete.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
