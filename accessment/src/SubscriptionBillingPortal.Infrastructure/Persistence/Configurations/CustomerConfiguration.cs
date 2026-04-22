using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionBillingPortal.Domain.Aggregates;
using SubscriptionBillingPortal.Domain.ValueObjects;

namespace SubscriptionBillingPortal.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Ignore(c => c.FullName);

        builder.OwnsOne(c => c.Email, email =>
        {
            email.Property(e => e.Value)
                .IsRequired()
                .HasMaxLength(320);
        });

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Ignore(c => c.DomainEvents);
    }
}
