using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Tenants;

internal sealed class PlatformRecoveryCodeConfiguration : IEntityTypeConfiguration<PlatformRecoveryCode>
{
    public void Configure(EntityTypeBuilder<PlatformRecoveryCode> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CodeHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(e => new { e.PlatformUserId, e.CodeHash }).IsUnique();
    }
}
