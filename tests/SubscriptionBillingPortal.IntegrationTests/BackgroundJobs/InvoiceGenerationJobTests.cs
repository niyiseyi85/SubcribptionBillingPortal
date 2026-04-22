using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.Enums;
using SubscriptionBillingPortal.Domain.ValueObjects;
using SubscriptionBillingPortal.Infrastructure.BackgroundJobs;
using SubscriptionBillingPortal.Infrastructure.Persistence;
using SubscriptionBillingPortal.IntegrationTests.Infrastructure;

namespace SubscriptionBillingPortal.IntegrationTests.BackgroundJobs;

/// <summary>
/// Integration tests for InvoiceGenerationJob.
/// Uses a real DI scope from the WebApplicationFactory to exercise the full
/// job pipeline: UnitOfWork → Subscription aggregate → Outbox write side.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class InvoiceGenerationJobTests
{
    private readonly CustomWebApplicationFactory _factory;

    public InvoiceGenerationJobTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RunBillingCycle_WithDueActiveSubscription_ShouldGenerateNewInvoice()
    {
        // Arrange — seed an active subscription and backdate NextBillingDate to the past
        using var scope = _factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var plan = SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly);
        var subscription = Subscription.Create(Guid.NewGuid(), plan);
        subscription.Activate(); // sets NextBillingDate = UtcNow + 30 days

        await unitOfWork.Subscriptions.AddAsync(subscription, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Manually backdate NextBillingDate so the job considers it due
        await db.Database.EnsureCreatedAsync();
        var tracked = await db.Set<Subscription>().FindAsync(subscription.Id);
        db.Entry(tracked!).Property("NextBillingDate").CurrentValue = DateTimeOffset.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        var invoiceCountBefore = subscription.Invoices.Count;

        // Act — create and run the job directly
        var job = new InvoiceGenerationJob(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<InvoiceGenerationJob>.Instance,
            _factory.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.RunOnceAsync(cts.Token);

        // Assert — reload from DB and verify a second invoice was generated
        using var verifyScope = _factory.Services.CreateScope();
        var verifyUow = verifyScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var reloaded = await verifyUow.Subscriptions.GetByIdAsync(subscription.Id, CancellationToken.None);

        reloaded.Should().NotBeNull();
        reloaded!.Invoices.Should().HaveCount(invoiceCountBefore + 1);
    }

    [Fact]
    public async Task RunBillingCycle_WithCancelledSubscription_ShouldNotGenerateInvoice()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var plan = SubscriptionPlan.Create(PlanType.Basic, BillingInterval.Monthly);
        var subscription = Subscription.Create(Guid.NewGuid(), plan);
        subscription.Activate();
        subscription.Cancel();

        await unitOfWork.Subscriptions.AddAsync(subscription, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var invoiceCountBefore = subscription.Invoices.Count;

        // Act
        var job = new InvoiceGenerationJob(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<InvoiceGenerationJob>.Instance,
            _factory.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.RunOnceAsync(cts.Token);

        // Assert — cancelled subscription must not receive new invoices
        using var verifyScope = _factory.Services.CreateScope();
        var verifyUow = verifyScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var reloaded = await verifyUow.Subscriptions.GetByIdAsync(subscription.Id, CancellationToken.None);

        reloaded!.Invoices.Should().HaveCount(invoiceCountBefore);
    }

    [Fact]
    public async Task RunBillingCycle_WithActiveSubscriptionNotYetDue_ShouldNotGenerateInvoice()
    {
        // Arrange — active subscription with NextBillingDate in the future
        using var scope = _factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var plan = SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly);
        var subscription = Subscription.Create(Guid.NewGuid(), plan);
        subscription.Activate(); // sets NextBillingDate = UtcNow + 30 days (in the future)

        await unitOfWork.Subscriptions.AddAsync(subscription, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var invoiceCountBefore = subscription.Invoices.Count;

        // Act
        var job = new InvoiceGenerationJob(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<InvoiceGenerationJob>.Instance,
            _factory.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.RunOnceAsync(cts.Token);

        // Assert — billing date is in the future, so no new invoice should be generated
        using var verifyScope = _factory.Services.CreateScope();
        var verifyUow = verifyScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var reloaded = await verifyUow.Subscriptions.GetByIdAsync(subscription.Id, CancellationToken.None);

        reloaded!.Invoices.Should().HaveCount(invoiceCountBefore);
    }

    [Fact]
    public async Task RunBillingCycle_WithMultipleDueSubscriptions_ShouldGenerateAllInvoices()
    {
        // Arrange — seed 3 active subscriptions all overdue
        using var scope = _factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var subs = new[]
        {
            Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Basic, BillingInterval.Monthly)),
            Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Basic, BillingInterval.Monthly)),
            Subscription.Create(Guid.NewGuid(), SubscriptionPlan.Create(PlanType.Basic, BillingInterval.Monthly))
        };

        foreach (var sub in subs)
        {
            sub.Activate();
            await unitOfWork.Subscriptions.AddAsync(sub, CancellationToken.None);
        }
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Backdate all three
        foreach (var sub in subs)
        {
            var tracked = await db.Set<Subscription>().FindAsync(sub.Id);
            db.Entry(tracked!).Property("NextBillingDate").CurrentValue = DateTimeOffset.UtcNow.AddDays(-1);
        }
        await db.SaveChangesAsync();

        // Act
        var job = new InvoiceGenerationJob(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<InvoiceGenerationJob>.Instance,
            _factory.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await job.RunOnceAsync(cts.Token);

        // Assert — each subscription should have gained one additional invoice
        using var verifyScope = _factory.Services.CreateScope();
        var verifyUow = verifyScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        foreach (var sub in subs)
        {
            var reloaded = await verifyUow.Subscriptions.GetByIdAsync(sub.Id, CancellationToken.None);
            reloaded!.Invoices.Should().HaveCount(2, because: $"subscription {sub.Id} was due for billing");
        }
    }

    [Fact]
    public async Task RunBillingCycle_WithDueSubscription_ShouldWriteOutboxMessage()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var plan = SubscriptionPlan.Create(PlanType.Enterprise, BillingInterval.Monthly);
        var subscription = Subscription.Create(Guid.NewGuid(), plan);
        subscription.Activate();

        await unitOfWork.Subscriptions.AddAsync(subscription, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Record outbox count before the job runs
        var outboxCountBefore = await db.OutboxMessages.CountAsync();

        // Backdate so the subscription is due
        var tracked = await db.Set<Subscription>().FindAsync(subscription.Id);
        db.Entry(tracked!).Property("NextBillingDate").CurrentValue = DateTimeOffset.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        // Act
        var job = new InvoiceGenerationJob(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<InvoiceGenerationJob>.Instance,
            _factory.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.RunOnceAsync(cts.Token);

        // Assert — at least one new outbox message should have been written (InvoiceGeneratedEvent)
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var outboxCountAfter = await verifyDb.OutboxMessages.CountAsync();

        outboxCountAfter.Should().BeGreaterThan(outboxCountBefore);
    }
}