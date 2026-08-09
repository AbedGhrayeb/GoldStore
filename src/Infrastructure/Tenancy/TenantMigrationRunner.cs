using System.Text.RegularExpressions;
using Application.Abstractions.Data;
using Application.Abstractions.Tenancy;
using Domain.Tenants;
using Infrastructure.Database;
using Infrastructure.Platform;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel;
using SharedKernel.Result;

namespace Infrastructure.Tenancy;

/// <summary>
/// Startup migration runner. Brings every active tenant schema up to date by replaying the
/// idempotent tenant script (a no-op for tenants already at the latest migration), adopts
/// legacy schemas that predate per-tenant migration history, and records the outcome in the
/// platform <see cref="TenantMigrationLog"/> table. One tenant failing never blocks the others.
/// </summary>
internal sealed partial class TenantMigrationRunner(
    IServiceScopeFactory scopeFactory,
    ITenantSchemaProvisioner schemaProvisioner,
    IConfiguration configuration,
    IDateTimeProvider dateTimeProvider,
    ILogger<TenantMigrationRunner> logger) : ITenantMigrationRunner
{
    private const string HistoryTable = "__EFMigrationsHistory";

    private readonly Lazy<string[]> _migrationIds = new(() => ResolveMigrationIds(scopeFactory));

    private readonly string _productVersion =
        typeof(DbContext).Assembly.GetName().Version?.ToString(3) ?? "10.0.0";

    public async Task<Success> RunAsync(CancellationToken cancellationToken = default)
    {
        List<Tenant> tenants;
        using (IServiceScope scope = scopeFactory.CreateScope())
        {
            IPlatformDbContext platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();
            tenants = await platform.Tenants
                .AsNoTracking()
                .Where(t => t.IsActive)
                .ToListAsync(cancellationToken);
        }

        string[] migrationIds = _migrationIds.Value;
        string latestMigrationId = migrationIds[^1];
        DateTimeOffset attemptedAtUtc = new(dateTimeProvider.UtcNow, TimeSpan.Zero);

        foreach (Tenant tenant in tenants)
        {
            try
            {
                await AdoptLegacySchemaIfNeededAsync(tenant, cancellationToken);

                await schemaProvisioner.ProvisionAsync(tenant.SchemaName, tenant.ConnectionString, cancellationToken);

                await RecordAsync(tenant.Id, latestMigrationId, TenantMigrationStatus.Applied, null, attemptedAtUtc, cancellationToken);

                logger.LogInformation(
                    "Tenant {Subdomain} ({Schema}) is up to date at migration {MigrationId}.",
                    tenant.Subdomain, tenant.SchemaName, latestMigrationId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Migration failed for tenant {Subdomain} ({Schema}).", tenant.Subdomain, tenant.SchemaName);

                await RecordAsync(tenant.Id, latestMigrationId, TenantMigrationStatus.Failed, ex.Message, attemptedAtUtc, cancellationToken);
            }
        }

        return Result.Success;
    }

    /// <summary>
    /// A schema with tables but no per-tenant history table (e.g. created before the history
    /// was scoped per tenant) cannot be replayed safely. Adopt it: create the history table
    /// and mark the current migrations as already applied, so future migrations apply on top.
    /// </summary>
    private async Task AdoptLegacySchemaIfNeededAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        string schemaName = tenant.SchemaName;
        if (!SchemaNameRegex().IsMatch(schemaName))
        {
            throw new ArgumentException($"Invalid tenant schema name '{schemaName}'.", nameof(schemaName));
        }

        string connectionString = tenant.ConnectionString
            ?? configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsureSchemaAsync(connection, schemaName, cancellationToken);

        if (await TableExistsAsync(connection, schemaName, HistoryTable, cancellationToken))
        {
            return;
        }

        if (!await SchemaHasTablesAsync(connection, schemaName, cancellationToken))
        {
            return; // Fresh schema — provisioning creates everything, including the history table.
        }

        await CreateHistoryTableAsync(connection, schemaName, cancellationToken);
        await InsertAppliedMigrationsAsync(connection, schemaName, _migrationIds.Value, _productVersion, cancellationToken);

        logger.LogInformation(
            "Adopted legacy tenant schema {Schema} — marked {Count} migrations as applied.",
            schemaName, _migrationIds.Value.Length);
    }

    private async Task RecordAsync(
        Guid tenantId,
        string migrationId,
        TenantMigrationStatus status,
        string? error,
        DateTimeOffset attemptedAtUtc,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IPlatformDbContext platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();

        TenantMigrationLog? log = await platform.TenantMigrationLogs
            .FirstOrDefaultAsync(l => l.TenantId == tenantId, cancellationToken);

        if (log is null)
        {
            Result<TenantMigrationLog> created = TenantMigrationLog.Create(tenantId, migrationId, attemptedAtUtc);
            if (created.IsError)
            {
                return;
            }

            log = created.Value;
            platform.TenantMigrationLogs.Add(log);
        }

        if (status == TenantMigrationStatus.Failed)
        {
            log.MarkFailed(migrationId, error ?? "Unknown error", attemptedAtUtc);
        }
        else
        {
            log.MarkApplied(migrationId, attemptedAtUtc);
        }

        await platform.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSchemaAsync(SqlConnection connection, string schemaName, CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = $"IF SCHEMA_ID(N'{schemaName}') IS NULL EXEC(N'CREATE SCHEMA [{schemaName}];');";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(
        SqlConnection connection, string schemaName, string tableName, CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT CASE WHEN OBJECT_ID(N'{schemaName}.{tableName}', N'U') IS NULL THEN 0 ELSE 1 END";
        return (int)(await command.ExecuteScalarAsync(cancellationToken))! == 1;
    }

    private static async Task<bool> SchemaHasTablesAsync(
        SqlConnection connection, string schemaName, CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID(N'{schemaName}')";
        return (int)(await command.ExecuteScalarAsync(cancellationToken))! > 0;
    }

    private static async Task CreateHistoryTableAsync(
        SqlConnection connection, string schemaName, CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE TABLE [{schemaName}].[{HistoryTable}] (
                [MigrationId] nvarchar(150) NOT NULL,
                [ProductVersion] nvarchar(32) NOT NULL,
                CONSTRAINT [PK__{HistoryTable}_Tenant] PRIMARY KEY ([MigrationId])
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertAppliedMigrationsAsync(
        SqlConnection connection, string schemaName, IReadOnlyList<string> migrationIds, string productVersion, CancellationToken cancellationToken)
    {
        string valueTuples = string.Join(", ", Enumerable.Range(0, migrationIds.Count).Select(i => $"(@m{i}, @v{i})"));

        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO [{schemaName}].[{HistoryTable}] ([MigrationId], [ProductVersion])
            SELECT source.[MigrationId], source.[ProductVersion]
            FROM (VALUES {valueTuples}) AS source([MigrationId], [ProductVersion])
            WHERE NOT EXISTS (
                SELECT 1 FROM [{schemaName}].[{HistoryTable}] WHERE [MigrationId] = source.[MigrationId]);
            """;

        for (int i = 0; i < migrationIds.Count; i++)
        {
            command.Parameters.AddWithValue($"@m{i}", migrationIds[i]);
            command.Parameters.AddWithValue($"@v{i}", productVersion);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string[] ResolveMigrationIds(IServiceScopeFactory scopeFactory)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        IMigrationsAssembly migrationsAssembly = context.GetService<IMigrationsAssembly>();
        return migrationsAssembly.Migrations.Keys.OrderBy(id => id).ToArray();
    }

    [GeneratedRegex("^[a-z][a-z0-9_]{0,127}$")]
    private static partial Regex SchemaNameRegex();
}
