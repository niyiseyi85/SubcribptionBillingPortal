using Microsoft.EntityFrameworkCore;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.Common;
using SubscriptionBillingPortal.Domain.Entities;

namespace SubscriptionBillingPortal.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the SubscriptionBillingPortal.
/// Uses InMemory provider. No business logic — pure persistence concern.
/// </summary>
public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
