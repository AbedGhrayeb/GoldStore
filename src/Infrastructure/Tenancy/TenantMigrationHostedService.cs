using Application.Abstractions.Tenancy;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Tenancy;

/// <summary>
/// Runs the tenant migration runner once at startup, after the platform database has been
/// initialized. Failures are logged; they never prevent the host from starting.
/// </summary>
internal sealed class TenantMigrationHostedService(
    ITenantMigrationRunner tenantMigrationRunner,
    ILogger<TenantMigrationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await tenantMigrationRunner.RunAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Tenant migration run failed during startup.");
        }
    }
}
