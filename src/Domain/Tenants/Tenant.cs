using System.Text.RegularExpressions;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Tenants;

public sealed partial class Tenant : Entity
{
    private static readonly HashSet<string> ReservedSubdomains = new(StringComparer.Ordinal)
    {
        "www", "api", "app", "admin", "platform", "mail", "support", "goldstore"
    };

    public string Name { get; private set; }
    public string Subdomain { get; private set; }
    public string SchemaName { get; private set; }
    public TenantStatus Status { get; private set; }
    public Guid PlanId { get; private set; }
    public string? SettingsJson { get; private set; }
    public DateTimeOffset? TrialEndsAtUtc { get; private set; }
    public DateTimeOffset? SubscriptionExpiresAtUtc { get; private set; }
    public string? ConnectionString { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Tenant() { }

    private Tenant(Guid id, string name, string subdomain, Guid planId, TenantStatus status, DateTimeOffset createdAtUtc, DateTimeOffset? trialEndsAtUtc)
        : base(id)
    {
        Name = name;
        Subdomain = subdomain;
        SchemaName = ToSchemaName(subdomain);
        PlanId = planId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        TrialEndsAtUtc = trialEndsAtUtc;
    }

    public static Result<Tenant> Create(string name, string subdomain, Guid planId, DateTimeOffset createdAtUtc, DateTimeOffset? trialEndsAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return TenantErrors.NameRequired;
        }

        string normalizedSubdomain = subdomain?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedSubdomain))
        {
            return TenantErrors.SubdomainRequired;
        }

        if (!SubdomainRegex().IsMatch(normalizedSubdomain))
        {
            return TenantErrors.SubdomainInvalid;
        }

        if (ReservedSubdomains.Contains(normalizedSubdomain))
        {
            return TenantErrors.SubdomainReserved;
        }

        if (planId == Guid.Empty)
        {
            return TenantErrors.PlanRequired;
        }

        TenantStatus status = trialEndsAtUtc.HasValue ? TenantStatus.Trial : TenantStatus.Active;

        return new Tenant(Guid.CreateVersion7(), name, normalizedSubdomain, planId, status, createdAtUtc, trialEndsAtUtc);
    }

    public Result<Updated> Update(string name, string? settingsJson)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return TenantErrors.NameRequired;
        }

        Name = name;
        SettingsJson = settingsJson;

        return Result.Updated;
    }

    public void ChangePlan(Guid planId)
    {
        PlanId = planId;
    }

    public Result<Updated> UpdateSettings(string? settingsJson)
    {
        SettingsJson = settingsJson;

        return Result.Updated;
    }

    public void ChangeStatus(TenantStatus status)
    {
        Status = status;
    }

    public void RenewSubscription(DateTimeOffset expiresAtUtc)
    {
        SubscriptionExpiresAtUtc = expiresAtUtc;
        Status = TenantStatus.Active;
    }

    public void SetConnectionString(string? connectionString)
    {
        ConnectionString = connectionString;
    }

    // SQL identifiers are always bracket-quoted when executed; additionally normalizing
    // hyphens keeps the generated schema name a simple, safe identifier.
    private static string ToSchemaName(string subdomain) => $"t_{subdomain.Replace('-', '_')}";

    [GeneratedRegex("^[a-z0-9]([a-z0-9-]{0,28}[a-z0-9])?$")]
    private static partial Regex SubdomainRegex();
}
