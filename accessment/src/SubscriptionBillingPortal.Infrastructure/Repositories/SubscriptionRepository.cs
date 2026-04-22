using Microsoft.EntityFrameworkCore;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.Enums;
using SubscriptionBillingPortal.Infrastructure.Persistence;

namespace SubscriptionBillingPortal.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of ISubscriptionRepository.
/// Loads the full Subscription aggregate including its Invoices child collection.
/// </summary>
public sealed class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ApplicationDbContext _context;

    public SubscriptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Subscription?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Include(s => s.Invoices)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Include(s => s.Invoices)
            .Where(s => s.Status == SubscriptionStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> GetAllDueForBillingAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Include(s => s.Invoices)
            .Where(s => s.Status == SubscriptionStatus.Active
                     && s.NextBillingDate.HasValue
                     && s.NextBillingDate.Value <= asOf)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        await _context.Subscriptions.AddAsync(subscription, cancellationToken);
    }
}

