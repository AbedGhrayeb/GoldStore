using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Tenancy;

/// <summary>
/// Schema-per-tenant means the model differs per context instance (different default schema),
/// so EF's model cache must be keyed by schema name in addition to the context type.
/// </summary>
internal sealed class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if (context is ApplicationDbContext applicationContext)
        {
            return (contextType: context.GetType(), schema: applicationContext.SchemaName, designTime);
        }

        return (contextType: context.GetType(), schema: (string?)null, designTime);
    }
}
