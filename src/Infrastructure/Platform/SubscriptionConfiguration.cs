using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Platform;

internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Interval).HasConversion<string>().HasMaxLength(20);

        builder.Property(s => s.Price).HasPrecision(18, 3);

        builder.Property(s => s.Currency).HasConversion<string>().HasMaxLength(3);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Plan>()
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.TenantId);
    }
}
