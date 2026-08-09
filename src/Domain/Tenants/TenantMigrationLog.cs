using SharedKernel;
using SharedKernel.Result;

namespace Domain.Tenants;

/// <summary>
/// Platform-level record of the latest migration state for a tenant schema.
/// The per-tenant EF history table holds the exact applied-migration list;
/// this table is the operations snapshot (when the last migration ran and whether it succeeded).
/// </summary>
public sealed class TenantMigrationLog : Entity
{
    public Guid TenantId { get; private set; }
    public string MigrationId { get; private set; }
    public TenantMigrationStatus Status { get; private set; }
    public DateTimeOffset AppliedAtUtc { get; private set; }
    public string? Error { get; private set; }

    private TenantMigrationLog() { }

    private TenantMigrationLog(Guid id, Guid tenantId, string migrationId, DateTimeOffset appliedAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        MigrationId = migrationId;
        Status = TenantMigrationStatus.Applied;
        AppliedAtUtc = appliedAtUtc;
    }

    public static Result<TenantMigrationLog> Create(Guid tenantId, string migrationId, DateTimeOffset appliedAtUtc)
    {
        if (tenantId == Guid.Empty)
        {
            return TenantMigrationLogErrors.TenantRequired;
        }

        if (string.IsNullOrWhiteSpace(migrationId))
        {
            return TenantMigrationLogErrors.MigrationIdRequired;
        }

        return new TenantMigrationLog(Guid.CreateVersion7(), tenantId, migrationId, appliedAtUtc);
    }

    public void MarkApplied(string migrationId, DateTimeOffset appliedAtUtc)
    {
        MigrationId = migrationId;
        Status = TenantMigrationStatus.Applied;
        AppliedAtUtc = appliedAtUtc;
        Error = null;
    }

    public void MarkFailed(string migrationId, string error, DateTimeOffset attemptedAtUtc)
    {
        MigrationId = migrationId;
        Status = TenantMigrationStatus.Failed;
        AppliedAtUtc = attemptedAtUtc;
        Error = error;
    }
}
