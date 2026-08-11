using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Tenants;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.Name).HasMaxLength(200).IsRequired();
        builder.Property(tenant => tenant.Key).HasMaxLength(63).IsRequired();
        builder.Property(tenant => tenant.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(tenant => tenant.Key).IsUnique();
    }
}
