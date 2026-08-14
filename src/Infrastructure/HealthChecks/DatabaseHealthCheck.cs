using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Infrastructure.HealthChecks;

internal sealed class DatabaseHealthCheck(
    ApplicationDbContext context,
    ILogger<DatabaseHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext healthCheckContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database health check failed.");

            return HealthCheckResult.Unhealthy("Database unreachable.", exception);
        }
    }
}
