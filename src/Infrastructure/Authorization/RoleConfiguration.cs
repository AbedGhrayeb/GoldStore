using Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Authorization;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(role => role.Id);
        builder.Property(role => role.Key).HasMaxLength(100).IsRequired();
        builder.Property(role => role.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(role => role.Key).IsUnique();

        builder.HasMany(role => role.RolePermissions)
            .WithOne()
            .HasForeignKey(rolePermission => rolePermission.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
