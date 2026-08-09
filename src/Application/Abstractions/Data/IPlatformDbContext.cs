using Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IPlatformDbContext
{
    DbSet<Tenant> Tenants { get; }

    DbSet<Plan> Plans { get; }

    DbSet<Subscription> Subscriptions { get; }

    DbSet<PlatformAdmin> PlatformAdmins { get; }

    DbSet<TenantMigrationLog> TenantMigrationLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
