using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SharedKernel;

namespace Infrastructure.Tenants;

/// <summary>
/// Applies tenant ownership to every <see cref="ITenantEntity"/> in the model:
/// a required foreign key to <see cref="Tenant"/> (restrict delete) and an index
/// starting with TenantId. Phase 3 extends this with the global tenant query filter.
/// </summary>
internal static class TenantOwnershipModelBuilderExtensions
{
    public static ModelBuilder ApplyTenantOwnership(this ModelBuilder modelBuilder)
    {
        IMutableEntityType tenantEntityType = modelBuilder.Model.FindEntityType(typeof(Tenant))
            ?? throw new InvalidOperationException("Tenant must be configured before applying tenant ownership.");
        IMutableKey tenantPrimaryKey = tenantEntityType.FindPrimaryKey()
            ?? throw new InvalidOperationException("Tenant has no primary key configured.");

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            IMutableProperty tenantIdProperty = entityType.FindProperty(nameof(ITenantEntity.TenantId))
                ?? throw new InvalidOperationException(
                    $"{entityType.ClrType.Name} implements ITenantEntity but has no mapped TenantId property.");

            bool hasTenantRelationship = tenantIdProperty.GetContainingForeignKeys()
                .Any(foreignKey => foreignKey.PrincipalEntityType == tenantEntityType);
            if (!hasTenantRelationship)
            {
                IMutableForeignKey foreignKey = entityType.AddForeignKey([tenantIdProperty], tenantPrimaryKey, tenantEntityType);
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }

            bool hasTenantIndex = entityType.GetIndexes()
                .Any(index => index.Properties.Count > 0 && index.Properties[0] == tenantIdProperty);
            if (!hasTenantIndex)
            {
                entityType.AddIndex(tenantIdProperty);
            }
        }

        return modelBuilder;
    }
}
