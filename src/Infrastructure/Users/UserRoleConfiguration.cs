using Domain.Authorization;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Users;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasKey(userRole => userRole.Id);
        builder.Property(userRole => userRole.UserId).IsRequired();
        builder.Property(userRole => userRole.RoleId).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(userRole => userRole.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // A role template can be assigned to a user at most once per tenant.
        builder.HasIndex(userRole => new { userRole.TenantId, userRole.UserId, userRole.RoleId }).IsUnique();
    }
}
