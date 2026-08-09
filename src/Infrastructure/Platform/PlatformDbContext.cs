using Application.Abstractions.Data;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Platform;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options), IPlatformDbContext
{
    public const string Schema = "platform";

    public DbSet<Tenant> Tenants { get; set; }

    public DbSet<Plan> Plans { get; set; }

    public DbSet<Subscription> Subscriptions { get; set; }

    public DbSet<PlatformAdmin> PlatformAdmins { get; set; }

    public DbSet<TenantMigrationLog> TenantMigrationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Only platform configurations — tenant configurations live in the same assembly
        // but must never leak into this model (and vice versa).
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PlatformDbContext).Assembly,
            type => type.Namespace == typeof(PlatformDbContext).Namespace);
    }
}
