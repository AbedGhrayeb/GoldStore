namespace Application.Abstractions.Tenancy;

/// <summary>
/// Creates (or brings up to date) a tenant's schema by replaying the idempotent
/// tenant migration script with the placeholder schema replaced. Safe to re-run.
/// </summary>
public interface ITenantSchemaProvisioner
{
    /// <param name="schemaName">Validated tenant schema name (e.g. t_demo).</param>
    /// <param name="connectionString">Dedicated database (escape hatch), or null for the shared database.</param>
    Task ProvisionAsync(string schemaName, string? connectionString, CancellationToken cancellationToken = default);
}
