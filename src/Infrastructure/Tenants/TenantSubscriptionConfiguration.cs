using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Tenants;

internal sealed class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(subscription => subscription.BillingCycle).HasConversion<string>().HasMaxLength(20);
        builder.Property(subscription => subscription.BillingProvider).HasMaxLength(100);
        builder.Property(subscription => subscription.BillingProviderReference).HasMaxLength(200);

        // The Tenant relationship is applied centrally by TenantOwnershipModelBuilderExtensions.
        builder.HasOne<SubscriptionPlan>().WithMany()
            .HasForeignKey(subscription => subscription.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(subscription => new { subscription.TenantId, subscription.EndsAtUtc });
    }
}
