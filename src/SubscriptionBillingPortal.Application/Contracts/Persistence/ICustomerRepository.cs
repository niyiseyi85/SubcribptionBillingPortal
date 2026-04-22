using SubscriptionBillingPortal.Domain.Aggregates;

namespace SubscriptionBillingPortal.Application.Contracts.Persistence;

/// <summary>
/// Repository contract for the Customer aggregate.
/// Implementations live in the Infrastructure layer.
/// </summary>
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid customerId, CancellationToken cancellationToken = default);
}
