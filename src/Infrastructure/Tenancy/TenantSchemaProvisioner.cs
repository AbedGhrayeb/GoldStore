using System.Text;
using System.Text.RegularExpressions;
using Application.Abstractions.Tenancy;
using Infrastructure.Database;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tenancy;

/// <summary>
/// Generates the full idempotent tenant schema script once (from the placeholder-schema
/// model), then replays it per tenant with the placeholder replaced by the tenant's schema.
/// The generated script carries its own per-migration transactions and IF-guards,
/// so re-running after a partial failure safely completes the schema.
/// </summary>
internal sealed partial class TenantSchemaProvisioner(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration) : ITenantSchemaProvisioner
{
    private readonly Lazy<string> _scriptTemplate = new(() => GenerateScriptTemplate(scopeFactory));

    public async Task ProvisionAsync(string schemaName, string? connectionString, CancellationToken cancellationToken = default)
    {
        // The schema name is embedded in DDL text (not parameterizable) — it must stay
        // a simple identifier. Tenant.Create already guarantees this via the subdomain regex.
        if (!SchemaNameRegex().IsMatch(schemaName))
        {
            throw new ArgumentException($"Invalid tenant schema name '{schemaName}'.", nameof(schemaName));
        }

        string script = _scriptTemplate.Value.Replace(
            ApplicationDbContext.PlaceholderSchema,
            schemaName,
            StringComparison.Ordinal);

        string effectiveConnectionString = connectionString
            ?? configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        await using var connection = new SqlConnection(effectiveConnectionString);
        await connection.OpenAsync(cancellationToken);

        // The script now creates its history table inside the tenant schema, so the schema
        // must exist before the first batch runs.
        await EnsureSchemaAsync(connection, schemaName, cancellationToken);

        foreach (string batch in SplitBatches(script))
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await using SqlCommand command = connection.CreateCommand();
            command.CommandText = batch;
            command.CommandTimeout = 120;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static string GenerateScriptTemplate(IServiceScopeFactory scopeFactory)
    {
        // No tenant is set in this scope, so the model is built with the placeholder schema.
        using IServiceScope scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return context.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
    }

    private static async Task EnsureSchemaAsync(
        SqlConnection connection, string schemaName, CancellationToken cancellationToken)
    {
        // CREATE SCHEMA must be the first statement in its batch.
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = $"IF SCHEMA_ID(N'{schemaName}') IS NULL EXEC(N'CREATE SCHEMA [{schemaName}];');";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // EF scripts separate batches with GO lines; ADO.NET executes one batch at a time.
    private static IEnumerable<string> SplitBatches(string script)
    {
        using var reader = new StringReader(script);
        var batch = new StringBuilder();

        while (reader.ReadLine() is { } line)
        {
            if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                yield return batch.ToString();
                batch.Clear();
            }
            else
            {
                batch.AppendLine(line);
            }
        }

        yield return batch.ToString();
    }

    [GeneratedRegex("^[a-z][a-z0-9_]{0,127}$")]
    private static partial Regex SchemaNameRegex();
}
