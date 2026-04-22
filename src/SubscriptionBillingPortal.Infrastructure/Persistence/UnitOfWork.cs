using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
        FixNewEntitiesTrackedAsModified();
        var result = await _context.SaveChangesAsync(cancellationToken);
        ClearDomainEvents();
        return result;
    }

    /// <summary>
    /// Corrects a subtle EF Core change-tracking quirk: entities added to a navigation
    /// backing field (e.g. Invoice added to Subscription._invoices via a domain method)
    /// are discovered during DetectChanges with <see cref="EntityState.Modified"/> instead
    /// of <see cref="EntityState.Added"/> because EF Core treats any non-default Guid PK
    /// as an existing record.  A brand-new entity has no prior DB snapshot, so every
    /// property's OriginalValue equals its CurrentValue — we use that invariant to
    /// identify and reclassify these entries before the actual SaveChanges call.
    /// </summary>
    private void FixNewEntitiesTrackedAsModified()
    {
        foreach (var entry in _context.ChangeTracker.Entries()
                     .Where(e => e.State == EntityState.Modified)
                     .ToList())
        {
            bool allPropertiesUnchanged = entry.Properties
                .All(p => Equals(p.OriginalValue, p.CurrentValue));

            if (allPropertiesUnchanged)
                entry.State = EntityState.Added;
        }
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

