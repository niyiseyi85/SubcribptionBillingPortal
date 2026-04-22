using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.Enums;
using SubscriptionBillingPortal.Domain.ValueObjects;

namespace SubscriptionBillingPortal.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.CustomerId)
            .IsRequired();

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.OwnsOne(s => s.Plan, plan =>
        {
            plan.Property(p => p.PlanType)
                .IsRequired()
                .HasConversion<string>();

            plan.Property(p => p.BillingInterval)
                .IsRequired()
                .HasConversion<string>();

            // Price is a Money value object nested inside the Plan owned entity.
            plan.OwnsOne(p => p.Price, money =>
            {
                money.Property(m => m.Amount).IsRequired();
                money.Property(m => m.Currency).IsRequired().HasMaxLength(3);
            });

            plan.Ignore(p => p.BillingIntervalDays);
        });

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.ActivatedAt);
        builder.Property(s => s.CancelledAt);
        builder.Property(s => s.LastBillingDate);
        builder.Property(s => s.NextBillingDate);

        builder.HasMany(s => s.Invoices)
            .WithOne()
            .HasForeignKey(i => i.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(s => s.DomainEvents);
    }
}
