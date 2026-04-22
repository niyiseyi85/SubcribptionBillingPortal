using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionBillingPortal.Domain.Entities;
using SubscriptionBillingPortal.Domain.ValueObjects;

namespace SubscriptionBillingPortal.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.SubscriptionId)
            .IsRequired();

        // Amount is a Money value object (amount + currency).
        builder.OwnsOne(i => i.Amount, money =>
        {
            money.Property(m => m.Amount).IsRequired();
            money.Property(m => m.Currency).IsRequired().HasMaxLength(3);
        });

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(i => i.IssuedAt)
            .IsRequired();

        builder.Property(i => i.PaidAt);

        builder.Property(i => i.PaymentReference)
            .HasMaxLength(200);

        // Unique index enforces that no two invoices share the same PaymentReference.
        // NULL values are excluded at the application level (PaymentReference is only set on payment),
        // so IsUnique() is sufficient — HasFilter is omitted for InMemory provider compatibility.
        builder.HasIndex(i => i.PaymentReference)
            .IsUnique();

        builder.Ignore(i => i.DomainEvents);
    }
}
