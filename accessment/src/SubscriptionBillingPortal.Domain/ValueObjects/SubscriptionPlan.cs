using SubscriptionBillingPortal.Domain.Enums;
using SubscriptionBillingPortal.Domain.Exceptions;

namespace SubscriptionBillingPortal.Domain.ValueObjects;

/// <summary>
/// Value object representing a subscription plan.
/// Encapsulates plan type, billing interval, and the canonical price as a domain business rule.
/// Sealed class (not record) to satisfy EF Core's owned-entity requirements.
/// All prices are fixed — no external pricing service is needed.
/// </summary>
public sealed class SubscriptionPlan
{
    // (PlanType, BillingInterval) → fixed price in USD
    private static readonly IReadOnlyDictionary<(PlanType, BillingInterval), Money> PricingTable =
        new Dictionary<(PlanType, BillingInterval), Money>
        {
            { (PlanType.Basic,      BillingInterval.Monthly),   Money.Of(  9.99m) },
            { (PlanType.Basic,      BillingInterval.Quarterly), Money.Of( 26.99m) },
            { (PlanType.Basic,      BillingInterval.Annual),    Money.Of( 99.99m) },

            { (PlanType.Pro,        BillingInterval.Monthly),   Money.Of( 29.99m) },
            { (PlanType.Pro,        BillingInterval.Quarterly), Money.Of( 79.99m) },
            { (PlanType.Pro,        BillingInterval.Annual),    Money.Of(299.99m) },

            { (PlanType.Enterprise, BillingInterval.Monthly),   Money.Of( 99.99m) },
            { (PlanType.Enterprise, BillingInterval.Quarterly), Money.Of(269.99m) },
            { (PlanType.Enterprise, BillingInterval.Annual),    Money.Of(999.99m) },
        };

    public PlanType PlanType { get; private set; }
    public BillingInterval BillingInterval { get; private set; }
    public Money Price { get; private set; } = null!;

    /// <summary>
    /// Number of days in the billing cycle — used by the aggregate to schedule NextBillingDate.
    /// Computed from <see cref="BillingInterval"/>; not persisted.
    /// </summary>
    public int BillingIntervalDays => BillingInterval switch
    {
        BillingInterval.Monthly   => 30,
        BillingInterval.Quarterly => 90,
        BillingInterval.Annual    => 365,
        _                         => 30
    };

    private SubscriptionPlan() { } // required by EF Core

    private SubscriptionPlan(PlanType planType, BillingInterval billingInterval, Money price)
    {
        PlanType = planType;
        BillingInterval = billingInterval;
        Price = price;
    }

    /// <summary>
    /// Creates a <see cref="SubscriptionPlan"/> for the given type and billing interval.
    /// Throws <see cref="DomainException"/> for any unsupported combination.
    /// </summary>
    public static SubscriptionPlan Create(PlanType planType, BillingInterval billingInterval)
    {
        if (!PricingTable.TryGetValue((planType, billingInterval), out var price))
        {
            throw new DomainException(
                $"No pricing configured for plan '{planType}' with interval '{billingInterval}'.");
        }

        return new SubscriptionPlan(planType, billingInterval, price);
    }
}
