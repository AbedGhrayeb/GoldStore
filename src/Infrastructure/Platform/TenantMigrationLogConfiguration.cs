using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Platform;

internal sealed class TenantMigrationLogConfiguration : IEntityTypeConfiguration<TenantMigrationLog>
{
    public void Configure(EntityTypeBuilder<TenantMigrationLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.MigrationId).HasMaxLength(150);

        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(l => l.Error).HasMaxLength(2000);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(l => l.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // One latest-state row per tenant.
        builder.HasIndex(l => l.TenantId).IsUnique();
    }
}
