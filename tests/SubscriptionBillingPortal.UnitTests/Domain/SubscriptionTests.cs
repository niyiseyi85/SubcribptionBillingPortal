using FluentAssertions;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.Enums;
using SubscriptionBillingPortal.Domain.Events;
using SubscriptionBillingPortal.Domain.Exceptions;
using SubscriptionBillingPortal.Domain.ValueObjects;

namespace SubscriptionBillingPortal.UnitTests.Domain;

/// <summary>
/// Unit tests for the Subscription aggregate.
/// Pure domain — no infrastructure dependencies.
/// </summary>
public sealed class SubscriptionTests
{
    // Pro/Monthly = $29.99 preserves the original assertion values.
    private static readonly SubscriptionPlan DefaultPlan =
        SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly);

    private const decimal DefaultPlanPrice = 29.99m;

    // ── Activate ──────────────────────────────────────────────────────────────

    [Fact]
    public void Activate_WhenSubscriptionIsInactive_ShouldSetStatusToActive()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);

        subscription.Activate();

        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void Activate_WhenSubscriptionIsInactive_ShouldSetActivatedAt()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        var before = DateTimeOffset.UtcNow;

        subscription.Activate();

        subscription.ActivatedAt.Should().NotBeNull();
        subscription.ActivatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Activate_WhenSubscriptionIsInactive_ShouldGenerateOneInvoice()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);

        subscription.Activate();

        subscription.Invoices.Should().HaveCount(1);
        subscription.Invoices.First().Status.Should().Be(InvoiceStatus.Pending);
        subscription.Invoices.First().SubscriptionId.Should().Be(subscription.Id);
    }

    [Fact]
    public void Activate_WhenSubscriptionIsInactive_ShouldSetInvoiceAmountToPlanPrice()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);

        subscription.Activate();

        subscription.Invoices.First().Amount.Should().Be(DefaultPlanPrice);
    }

    [Fact]
    public void Activate_WhenSubscriptionIsInactive_ShouldRaiseSubscriptionActivatedEvent()
    {
        var customerId = Guid.NewGuid();
        var subscription = Subscription.Create(customerId, DefaultPlan);

        subscription.Activate();

        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionActivatedEvent);
        var @event = (SubscriptionActivatedEvent)subscription.DomainEvents.First(e => e is SubscriptionActivatedEvent);
        @event.SubscriptionId.Should().Be(subscription.Id);
        @event.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public void Activate_WhenSubscriptionIsInactive_ShouldRaiseInvoiceGeneratedEvent()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);

        subscription.Activate();

        subscription.DomainEvents.Should().ContainSingle(e => e is InvoiceGeneratedEvent);
    }

    [Fact]
    public void Activate_WhenSubscriptionIsAlreadyActive_ShouldThrowDomainException()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();

        var act = () => subscription.Activate();

        act.Should().Throw<DomainException>()
            .WithMessage("*already active*");
    }

    [Fact]
    public void Activate_WhenSubscriptionIsCancelled_ShouldThrowDomainException()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        subscription.Cancel();

        var act = () => subscription.Activate();

        act.Should().Throw<DomainException>()
            .WithMessage("*cancelled*");
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_WhenSubscriptionIsActive_ShouldSetStatusToCancelled()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();

        subscription.Cancel();

        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenSubscriptionIsActive_ShouldSetCancelledAt()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        var before = DateTimeOffset.UtcNow;

        subscription.Cancel();

        subscription.CancelledAt.Should().NotBeNull();
        subscription.CancelledAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Cancel_WhenSubscriptionIsActive_ShouldRaiseSubscriptionCancelledEvent()
    {
        var customerId = Guid.NewGuid();
        var subscription = Subscription.Create(customerId, DefaultPlan);
        subscription.Activate();
        subscription.ClearDomainEvents();

        subscription.Cancel();

        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionCancelledEvent);
        var @event = (SubscriptionCancelledEvent)subscription.DomainEvents.First(e => e is SubscriptionCancelledEvent);
        @event.SubscriptionId.Should().Be(subscription.Id);
        @event.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public void Cancel_WhenSubscriptionIsAlreadyCancelled_ShouldThrowDomainException()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        subscription.Cancel();

        var act = () => subscription.Cancel();

        act.Should().Throw<DomainException>()
            .WithMessage("*already cancelled*");
    }

    // ── GenerateInvoice ───────────────────────────────────────────────────────

    [Fact]
    public void GenerateInvoice_WhenSubscriptionIsActive_ShouldAddInvoice()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate(); // invoice #1

        subscription.GenerateInvoice(); // invoice #2

        subscription.Invoices.Should().HaveCount(2);
    }

    [Fact]
    public void GenerateInvoice_WhenSubscriptionIsNotActive_ShouldThrowDomainException()
    {
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);

        var act = () => subscription.GenerateInvoice();

        act.Should().Throw<DomainException>()
            .WithMessage("*not active*");
    }

    // ── SubscriptionPlan ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(PlanType.Basic,      BillingInterval.Monthly,    9.99)]
    [InlineData(PlanType.Basic,      BillingInterval.Quarterly, 26.99)]
    [InlineData(PlanType.Basic,      BillingInterval.Annual,    99.99)]
    [InlineData(PlanType.Pro,        BillingInterval.Monthly,   29.99)]
    [InlineData(PlanType.Pro,        BillingInterval.Quarterly, 79.99)]
    [InlineData(PlanType.Pro,        BillingInterval.Annual,   299.99)]
    [InlineData(PlanType.Enterprise, BillingInterval.Monthly,   99.99)]
    [InlineData(PlanType.Enterprise, BillingInterval.Quarterly,269.99)]
    [InlineData(PlanType.Enterprise, BillingInterval.Annual,   999.99)]
    public void SubscriptionPlan_Create_ShouldReturnCorrectPrice(
        PlanType planType, BillingInterval billingInterval, decimal expectedPrice)
    {
        var plan = SubscriptionPlan.Create(planType, billingInterval);

        plan.Price.Should().Be(expectedPrice);
    }

    [Theory]
    [InlineData(BillingInterval.Monthly,    30)]
    [InlineData(BillingInterval.Quarterly,  90)]
    [InlineData(BillingInterval.Annual,    365)]
    public void SubscriptionPlan_BillingIntervalDays_ShouldReturnCorrectDays(
        BillingInterval billingInterval, int expectedDays)
    {
        var plan = SubscriptionPlan.Create(PlanType.Basic, billingInterval);

        plan.BillingIntervalDays.Should().Be(expectedDays);
    }

    [Fact]
    public void Activate_WithAnnualPlan_ShouldSetNextBillingDateTo365DaysFromNow()
    {
        var plan = SubscriptionPlan.Create(PlanType.Enterprise, BillingInterval.Annual);
        var subscription = Subscription.Create(Guid.NewGuid(), plan);
        var before = DateTimeOffset.UtcNow;

        subscription.Activate();

        subscription.NextBillingDate.Should().NotBeNull();
        subscription.NextBillingDate!.Value.Should()
            .BeCloseTo(before.AddDays(365), TimeSpan.FromSeconds(5));
    }
}
