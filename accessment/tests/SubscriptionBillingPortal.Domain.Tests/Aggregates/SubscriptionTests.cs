using FluentAssertions;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.Enums;
using SubscriptionBillingPortal.Domain.Events;
using SubscriptionBillingPortal.Domain.Exceptions;
using SubscriptionBillingPortal.Domain.ValueObjects;

namespace SubscriptionBillingPortal.Domain.Tests.Aggregates;

/// <summary>
/// Domain tests for the Subscription aggregate.
/// Tests enforce business rules without any infrastructure dependencies.
/// </summary>
public sealed class SubscriptionTests
{
    // Pro/Monthly = $29.99 — matches the legacy DefaultPlanAmount used in amount assertions.
    private static readonly SubscriptionPlan DefaultPlan =
        SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly);

    private const decimal DefaultPlanPrice = 29.99m;

    // ── Activate ──────────────────────────────────────────────────────────────

    [Fact]
    public void Activate_WhenSubscriptionIsInactive_ShouldSetStatusToActive()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);

        // Act
        subscription.Activate();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void Activate_WhenSubscriptionIsInactive_ShouldSetActivatedAt()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        var before = DateTimeOffset.UtcNow;

        // Act
        subscription.Activate();

        // Assert
        subscription.ActivatedAt.Should().NotBeNull();
        subscription.ActivatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Activate_WhenSubscriptionIsInactive_ShouldGenerateOneInvoice()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);

        // Act
        subscription.Activate();

        // Assert
        subscription.Invoices.Should().HaveCount(1);
        subscription.Invoices.First().Status.Should().Be(InvoiceStatus.Pending);
        subscription.Invoices.First().SubscriptionId.Should().Be(subscription.Id);
    }

    [Fact]
    public void Activate_WhenSubscriptionIsInactive_ShouldSetInvoiceAmountToPlanPrice()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);

        // Act
        subscription.Activate();

        // Assert
        subscription.Invoices.First().Amount.Amount.Should().Be(DefaultPlanPrice);
    }

    [Fact]
    public void Activate_WhenSubscriptionIsInactive_ShouldRaiseSubscriptionActivatedEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var subscription = Subscription.Create(customerId, DefaultPlan);

        // Act
        subscription.Activate();

        // Assert
        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionActivatedEvent);
        var activatedEvent = (SubscriptionActivatedEvent)subscription.DomainEvents.First(e => e is SubscriptionActivatedEvent);
        activatedEvent.SubscriptionId.Should().Be(subscription.Id);
        activatedEvent.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public void Activate_WhenSubscriptionIsInactive_ShouldRaiseInvoiceGeneratedEvent()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);

        // Act
        subscription.Activate();

        // Assert
        subscription.DomainEvents.Should().ContainSingle(e => e is InvoiceGeneratedEvent);
    }

    [Fact]
    public void Activate_WhenSubscriptionIsAlreadyActive_ShouldThrowDomainException()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();

        // Act
        var act = () => subscription.Activate();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*already active*");
    }

    [Fact]
    public void Activate_WhenSubscriptionIsCancelled_ShouldThrowDomainException()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        subscription.Cancel();

        // Act
        var act = () => subscription.Activate();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*cancelled*");
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_WhenSubscriptionIsActive_ShouldSetStatusToCancelled()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();

        // Act
        subscription.Cancel();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenSubscriptionIsActive_ShouldSetCancelledAt()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        var before = DateTimeOffset.UtcNow;

        // Act
        subscription.Cancel();

        // Assert
        subscription.CancelledAt.Should().NotBeNull();
        subscription.CancelledAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Cancel_WhenSubscriptionIsActive_ShouldRaiseSubscriptionCancelledEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var subscription = Subscription.Create(customerId, DefaultPlan);
        subscription.Activate();
        subscription.ClearDomainEvents();

        // Act
        subscription.Cancel();

        // Assert
        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionCancelledEvent);
        var cancelledEvent = (SubscriptionCancelledEvent)subscription.DomainEvents.First(e => e is SubscriptionCancelledEvent);
        cancelledEvent.SubscriptionId.Should().Be(subscription.Id);
        cancelledEvent.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public void Cancel_WhenSubscriptionIsAlreadyCancelled_ShouldThrowDomainException()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        subscription.Cancel();

        // Act
        var act = () => subscription.Cancel();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*already cancelled*");
    }

    [Fact]
    public void Cancel_WhenSubscriptionIsActive_ShouldVoidPendingInvoices()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate(); // creates 1 pending invoice

        // Act
        subscription.Cancel();

        // Assert
        subscription.Invoices.Should().AllSatisfy(i =>
            i.Status.Should().Be(InvoiceStatus.Voided));
    }

    [Fact]
    public void Cancel_WhenPaidInvoiceExists_ShouldNotVoidPaidInvoice()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        subscription.PayInvoice(subscription.Invoices.First().Id, "ref-001"); // paid
        subscription.GenerateInvoice(); // new pending invoice

        // Act
        subscription.Cancel();

        // Assert
        subscription.Invoices.Should().ContainSingle(i => i.Status == InvoiceStatus.Paid);
        subscription.Invoices.Should().ContainSingle(i => i.Status == InvoiceStatus.Voided);
    }

    // ── GenerateInvoice ───────────────────────────────────────────────────────

    [Fact]
    public void GenerateInvoice_WhenSubscriptionIsActive_ShouldAddInvoice()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate(); // invoice #1

        // Act
        subscription.GenerateInvoice(); // invoice #2

        // Assert
        subscription.Invoices.Should().HaveCount(2);
    }

    [Fact]
    public void GenerateInvoice_WhenSubscriptionIsNotActive_ShouldThrowDomainException()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);

        // Act
        var act = () => subscription.GenerateInvoice();

        // Assert
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
        plan.Price.Amount.Should().Be(expectedPrice);
    }

    [Theory]
    [InlineData(BillingInterval.Monthly,   30)]
    [InlineData(BillingInterval.Quarterly, 90)]
    [InlineData(BillingInterval.Annual,   365)]
    public void SubscriptionPlan_BillingIntervalDays_ShouldReturnCorrectDays(
        BillingInterval billingInterval, int expectedDays)
    {
        var plan = SubscriptionPlan.Create(PlanType.Basic, billingInterval);
        plan.BillingIntervalDays.Should().Be(expectedDays);
    }

    [Fact]
    public void Activate_WithAnnualPlan_ShouldSetNextBillingDateTo365DaysFromNow()
    {
        // Arrange
        var plan = SubscriptionPlan.Create(PlanType.Enterprise, BillingInterval.Annual);
        var subscription = Subscription.Create(Guid.NewGuid(), plan);
        var before = DateTimeOffset.UtcNow;

        // Act
        subscription.Activate();

        // Assert
        subscription.NextBillingDate.Should().NotBeNull();
        subscription.NextBillingDate!.Value.Should()
            .BeCloseTo(before.AddDays(365), TimeSpan.FromSeconds(5));
    }
}
