using Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Authorization;

internal sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.HasKey(up => up.Id);
        builder.Property(up => up.TenantId).IsRequired();
        builder.Property(up => up.UserId).IsRequired();
        builder.Property(up => up.PermissionId).IsRequired();

        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(up => up.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(up => new { up.TenantId, up.UserId, up.PermissionId }).IsUnique();
    }
}
