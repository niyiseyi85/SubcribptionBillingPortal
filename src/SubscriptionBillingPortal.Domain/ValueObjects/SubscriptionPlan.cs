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
    // (PlanType, BillingInterval) → monthly price in USD
    private static readonly IReadOnlyDictionary<(PlanType, BillingInterval), decimal> PricingTable =
        new Dictionary<(PlanType, BillingInterval), decimal>
        {
            { (PlanType.Basic,      BillingInterval.Monthly),   9.99m  },
            { (PlanType.Basic,      BillingInterval.Quarterly), 26.99m },
            { (PlanType.Basic,      BillingInterval.Annual),    99.99m },

            { (PlanType.Pro,        BillingInterval.Monthly),   29.99m  },
            { (PlanType.Pro,        BillingInterval.Quarterly),  79.99m },
            { (PlanType.Pro,        BillingInterval.Annual),    299.99m },

            { (PlanType.Enterprise, BillingInterval.Monthly),   99.99m  },
            { (PlanType.Enterprise, BillingInterval.Quarterly), 269.99m },
            { (PlanType.Enterprise, BillingInterval.Annual),    999.99m },
        };

    public PlanType PlanType { get; private set; }
    public BillingInterval BillingInterval { get; private set; }
    public decimal Price { get; private set; }

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

    private SubscriptionPlan(PlanType planType, BillingInterval billingInterval, decimal price)
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
