using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Platform;

internal sealed class PlatformAdminConfiguration : IEntityTypeConfiguration<PlatformAdmin>
{
    public void Configure(EntityTypeBuilder<PlatformAdmin> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName).HasMaxLength(100);

        builder.Property(p => p.LastName).HasMaxLength(100);

        builder.HasIndex(p => p.Email).IsUnique();
    }
}
