using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionBillingPortal.Domain.Common;

namespace SubscriptionBillingPortal.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.HasKey(i => i.IdempotencyKey);

        builder.Property(i => i.CommandName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.CreatedAt)
            .IsRequired();
    }
}
