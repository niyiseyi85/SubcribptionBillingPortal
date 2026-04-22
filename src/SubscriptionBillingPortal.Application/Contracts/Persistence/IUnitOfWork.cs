namespace SubscriptionBillingPortal.Application.Contracts.Persistence;

/// <summary>
/// Unit of Work — coordinates persistence across repositories within a single transaction boundary.
/// </summary>
public interface IUnitOfWork
{
    ICustomerRepository Customers { get; }
    ISubscriptionRepository Subscriptions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
