using System.Text.Json;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Domain.Common;
using SubscriptionBillingPortal.Infrastructure.Persistence;
using SubscriptionBillingPortal.Infrastructure.Repositories;

namespace SubscriptionBillingPortal.Infrastructure.Persistence;

/// <summary>
/// Unit of Work — coordinates the lifecycle of repositories and ensures all changes
/// are committed atomically.
///
/// OUTBOX WRITE SIDE: Before saving, all domain events are extracted from tracked
/// aggregates, serialized to JSON, and persisted as OutboxMessage rows in the same
/// database transaction. This guarantees at-least-once delivery: events are never
/// lost even if the process crashes immediately after SaveChanges.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public ICustomerRepository Customers { get; }
    public ISubscriptionRepository Subscriptions { get; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Customers = new CustomerRepository(context);
        Subscriptions = new SubscriptionRepository(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        WriteOutboxMessages();
        var result = await _context.SaveChangesAsync(cancellationToken);
        ClearDomainEvents();
        return result;
    }

    /// <summary>
    /// Scans all tracked BaseEntity instances for pending domain events,
    /// serializes each one, and creates a corresponding OutboxMessage row.
    /// This runs inside the same EF Core transaction as the aggregate state change.
    /// </summary>
    private void WriteOutboxMessages()
    {
        var domainEvents = _context.ChangeTracker
            .Entries<BaseEntity>()
            .SelectMany(entry => entry.Entity.DomainEvents)
            .ToList();

        if (domainEvents.Count == 0)
        {
            return;
        }

        var outboxMessages = domainEvents.Select(domainEvent => OutboxMessage.Create(
            eventType: domainEvent.GetType().AssemblyQualifiedName!,
            payload: JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions)));

        _context.OutboxMessages.AddRange(outboxMessages);
    }

    /// <summary>
    /// Clears in-memory domain events from all tracked entities after a successful save.
    /// Prevents events from being written to the outbox a second time on a subsequent save.
    /// </summary>
    private void ClearDomainEvents()
    {
        var entities = _context.ChangeTracker
            .Entries<BaseEntity>()
            .Select(entry => entry.Entity)
            .ToList();

        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }
    }
}

