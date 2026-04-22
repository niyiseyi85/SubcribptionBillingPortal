using FluentAssertions;
using SubscriptionBillingPortal.Domain.Enums;
using SubscriptionBillingPortal.Domain.Exceptions;
using SubscriptionBillingPortal.Domain.ValueObjects;

namespace SubscriptionBillingPortal.Domain.Tests.ValueObjects;

/// <summary>
/// Tests for the SubscriptionPlan value object.
/// Validates pricing rules and guard invariants.
/// </summary>
public sealed class SubscriptionPlanTests
{
    [Fact]
    public void Create_WithUnsupportedPlanTypeCombination_ShouldThrowDomainException()
    {
        // The enum values exist but no pricing entry is defined for this cast —
        // simulate by casting an out-of-range integer to PlanType.
        var unsupportedPlanType = (PlanType)999;

        // Act
        var act = () => SubscriptionPlan.Create(unsupportedPlanType, BillingInterval.Monthly);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*No pricing configured*");
    }

    [Fact]
    public void Create_WithUnsupportedBillingIntervalCombination_ShouldThrowDomainException()
    {
        // Arrange
        var unsupportedInterval = (BillingInterval)999;

        // Act
        var act = () => SubscriptionPlan.Create(PlanType.Basic, unsupportedInterval);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*No pricing configured*");
    }

    [Fact]
    public void Create_ShouldReturnPlanWithCorrectProperties()
    {
        // Act
        var plan = SubscriptionPlan.Create(PlanType.Pro, BillingInterval.Monthly);

        // Assert
        plan.PlanType.Should().Be(PlanType.Pro);
        plan.BillingInterval.Should().Be(BillingInterval.Monthly);
        plan.Price.Should().Be(29.99m);
        plan.BillingIntervalDays.Should().Be(30);
    }
}
