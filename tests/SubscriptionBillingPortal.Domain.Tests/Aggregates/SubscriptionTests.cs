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
        subscription.Invoices.First().Amount.Should().Be(DefaultPlanPrice);
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
        plan.Price.Should().Be(expectedPrice);
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

    [Fact]
    public void Activate_WithMonthlyPlan_ShouldSetNextBillingDateTo30DaysFromNow()
    {
        // Arrange
        var plan = SubscriptionPlan.Create(PlanType.Basic, BillingInterval.Monthly);
        var subscription = Subscription.Create(Guid.NewGuid(), plan);
        var before = DateTimeOffset.UtcNow;

        // Act
        subscription.Activate();

        // Assert
        subscription.NextBillingDate!.Value.Should()
            .BeCloseTo(before.AddDays(30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Activate_WithQuarterlyPlan_ShouldSetNextBillingDateTo90DaysFromNow()
    {
        // Arrange
        var plan = SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Quarterly);
        var subscription = Subscription.Create(Guid.NewGuid(), plan);
        var before = DateTimeOffset.UtcNow;

        // Act
        subscription.Activate();

        // Assert
        subscription.NextBillingDate!.Value.Should()
            .BeCloseTo(before.AddDays(90), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Activate_ShouldSetLastBillingDate()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        var before = DateTimeOffset.UtcNow;

        // Act
        subscription.Activate();

        // Assert
        subscription.LastBillingDate.Should().NotBeNull();
        subscription.LastBillingDate!.Value.Should().BeOnOrAfter(before);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldReturnSubscriptionWithInactiveStatus()
    {
        // Act
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Inactive);
        subscription.ActivatedAt.Should().BeNull();
        subscription.CancelledAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldReturnSubscriptionWithNoInvoices()
    {
        // Act
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);

        // Assert
        subscription.Invoices.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldAssignCorrectCustomerId()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var subscription = Subscription.Create(customerId, DefaultPlan);

        // Assert
        subscription.CustomerId.Should().Be(customerId);
    }

    // ── Cancel (edge cases) ───────────────────────────────────────────────────

    [Fact]
    public void Cancel_WhenSubscriptionIsInactive_ShouldSetStatusToCancelled()
    {
        // Arrange — never activated
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);

        // Act — domain allows cancelling an inactive subscription
        subscription.Cancel();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    // ── GenerateInvoice (explicit Cancelled case) ─────────────────────────────

    [Fact]
    public void GenerateInvoice_WhenSubscriptionIsCancelled_ShouldThrowDomainException()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        subscription.Cancel();

        // Act
        var act = () => subscription.GenerateInvoice();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*not active*");
    }

    // ── PayInvoice ────────────────────────────────────────────────────────────

    [Fact]
    public void PayInvoice_WithPendingInvoice_ShouldSetInvoiceStatusToPaid()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        var invoice = subscription.Invoices.First();

        // Act
        subscription.PayInvoice(invoice.Id, "ref-001");

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAt.Should().NotBeNull();
        invoice.PaymentReference.Should().Be("ref-001");
    }

    [Fact]
    public void PayInvoice_WithPendingInvoice_ShouldRaisePaymentReceivedEvent()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        subscription.ClearDomainEvents();
        var invoice = subscription.Invoices.First();

        // Act
        subscription.PayInvoice(invoice.Id, "ref-001");

        // Assert
        subscription.DomainEvents.Should().ContainSingle(e => e is PaymentReceivedEvent);
        var @event = (PaymentReceivedEvent)subscription.DomainEvents.First(e => e is PaymentReceivedEvent);
        @event.InvoiceId.Should().Be(invoice.Id);
        @event.SubscriptionId.Should().Be(subscription.Id);
    }

    [Fact]
    public void PayInvoice_WithSamePaymentReference_OnAlreadyPaidInvoice_ShouldBeNoOp()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        var invoice = subscription.Invoices.First();
        subscription.PayInvoice(invoice.Id, "ref-001");
        subscription.ClearDomainEvents();

        // Act — same reference → idempotent no-op
        var act = () => subscription.PayInvoice(invoice.Id, "ref-001");

        // Assert — no exception, no additional event
        act.Should().NotThrow();
        subscription.DomainEvents.Should().BeEmpty();
        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public void PayInvoice_WithDifferentPaymentReference_OnAlreadyPaidInvoice_ShouldThrowDomainException()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        var invoice = subscription.Invoices.First();
        subscription.PayInvoice(invoice.Id, "ref-001");

        // Act
        var act = () => subscription.PayInvoice(invoice.Id, "ref-002");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*already been paid*");
    }

    [Fact]
    public void PayInvoice_WithNonExistentInvoiceId_ShouldThrowDomainException()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();

        // Act
        var act = () => subscription.PayInvoice(Guid.NewGuid(), "ref-001");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void PayInvoice_WithEmptyPaymentReference_ShouldThrowDomainException()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        var invoice = subscription.Invoices.First();

        // Act
        var act = () => subscription.PayInvoice(invoice.Id, string.Empty);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*required*");
    }

    [Fact]
    public void PayInvoice_WithWhitespacePaymentReference_ShouldThrowDomainException()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        var invoice = subscription.Invoices.First();

        // Act
        var act = () => subscription.PayInvoice(invoice.Id, "   ");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*required*");
    }

    // ── GenerateInvoice (billing cycle advance) ───────────────────────────────

    [Fact]
    public void GenerateInvoice_WhenActive_ShouldAdvanceNextBillingDateByPlanInterval()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan); // Monthly = 30 days
        subscription.Activate();
        var previousNextBillingDate = subscription.NextBillingDate!.Value;
        var before = DateTimeOffset.UtcNow;

        // Act
        subscription.GenerateInvoice();

        // Assert — NextBillingDate should move forward by 30 days from now, NOT from the previous date
        subscription.NextBillingDate!.Value.Should()
            .BeCloseTo(before.AddDays(30), TimeSpan.FromSeconds(5));
        subscription.NextBillingDate!.Value.Should().BeAfter(previousNextBillingDate);
    }

    [Fact]
    public void GenerateInvoice_WhenActive_ShouldUpdateLastBillingDate()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        var activationBillingDate = subscription.LastBillingDate!.Value;
        var before = DateTimeOffset.UtcNow;

        // Act
        subscription.GenerateInvoice();

        // Assert
        subscription.LastBillingDate.Should().NotBeNull();
        subscription.LastBillingDate!.Value.Should().BeOnOrAfter(before);
        subscription.LastBillingDate!.Value.Should().BeOnOrAfter(activationBillingDate);
    }

    [Fact]
    public void GenerateInvoice_WhenActive_ShouldRaiseInvoiceGeneratedEvent()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        subscription.ClearDomainEvents();

        // Act
        subscription.GenerateInvoice();

        // Assert
        subscription.DomainEvents.Should().ContainSingle(e => e is InvoiceGeneratedEvent);
        var ev = (InvoiceGeneratedEvent)subscription.DomainEvents.First(e => e is InvoiceGeneratedEvent);
        ev.SubscriptionId.Should().Be(subscription.Id);
    }

    // ── Event counts ──────────────────────────────────────────────────────────

    [Fact]
    public void Activate_ShouldRaiseExactlyTwoEvents()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);

        // Act
        subscription.Activate();

        // Assert — exactly SubscriptionActivatedEvent + InvoiceGeneratedEvent
        subscription.DomainEvents.Should().HaveCount(2);
        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionActivatedEvent);
        subscription.DomainEvents.Should().ContainSingle(e => e is InvoiceGeneratedEvent);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllRaisedEvents()
    {
        // Arrange
        var subscription = Subscription.Create(Guid.NewGuid(), DefaultPlan);
        subscription.Activate();
        subscription.DomainEvents.Should().NotBeEmpty();

        // Act
        subscription.ClearDomainEvents();

        // Assert
        subscription.DomainEvents.Should().BeEmpty();
    }

    // ── Cancel raises event regardless of prior state ─────────────────────────

    [Fact]
    public void Cancel_WhenInactive_ShouldRaiseCancelledEvent()
    {
        // Arrange — subscription never activated
        var customerId = Guid.NewGuid();
        var subscription = Subscription.Create(customerId, DefaultPlan);

        // Act
        subscription.Cancel();

        // Assert — the cancelled event is raised even when transitioning from Inactive
        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionCancelledEvent);
        var ev = (SubscriptionCancelledEvent)subscription.DomainEvents.First(e => e is SubscriptionCancelledEvent);
        ev.SubscriptionId.Should().Be(subscription.Id);
        ev.CustomerId.Should().Be(customerId);
    }
}
