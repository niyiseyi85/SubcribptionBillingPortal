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
}
