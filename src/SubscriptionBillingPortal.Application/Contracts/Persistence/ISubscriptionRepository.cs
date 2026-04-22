using SubscriptionBillingPortal.Domain.Aggregates;

namespace SubscriptionBillingPortal.Application.Contracts.Persistence;

/// <summary>
/// Repository contract for the Subscription aggregate.
/// Implementations live in the Infrastructure layer.
/// </summary>
public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns only Active subscriptions whose NextBillingDate is on or before <paramref name="asOf"/>.
    /// Used exclusively by the billing background job to avoid generating premature invoices.
    /// </summary>
    Task<IReadOnlyList<Subscription>> GetAllDueForBillingAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default);

    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);
}
