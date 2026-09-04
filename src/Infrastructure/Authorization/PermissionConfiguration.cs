using Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Authorization;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(permission => permission.Id);
        builder.Property(permission => permission.Key).HasMaxLength(100).IsRequired();
        builder.Property(permission => permission.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(permission => permission.Key).IsUnique();
    }
}
