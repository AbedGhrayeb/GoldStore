using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Platform;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(200);

        builder.Property(t => t.Subdomain).HasMaxLength(63);

        builder.Property(t => t.SchemaName).HasMaxLength(128);

        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(t => t.ConnectionString).HasMaxLength(512);

        builder.HasOne<Plan>()
            .WithMany()
            .HasForeignKey(t => t.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.Subdomain).IsUnique();

        builder.HasIndex(t => t.SchemaName).IsUnique();
    }
}
