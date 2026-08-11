using SharedKernel;
using SharedKernel.Result;

namespace Domain.Tenants;

public sealed class Tenant : AuditableEntity
{
    public string Name { get; private set; }

    public string Key { get; private set; }

    public TenantStatus Status { get; private set; }

    public DateTimeOffset? TrialEndsAtUtc { get; private set; }

    public DateTimeOffset? CancellationReadOnlyUntilUtc { get; private set; }

    private Tenant()
    {
        Name = string.Empty;
        Key = string.Empty;
    }

    private Tenant(Guid id, string name, string key, TenantStatus status) : base(id)
    {
        Name = name;
        Key = key;
        Status = status;
    }

    public static Result<Tenant> Create(string name, string key, TenantStatus status)
    {
        return Create(Guid.CreateVersion7(), name, key, status);
    }

    public static Result<Tenant> Create(Guid id, string name, string key, TenantStatus status)
    {
        if (id == Guid.Empty)
        {
            return TenantErrors.IdRequired;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return TenantErrors.NameRequired;
        }

        string normalizedKey = (key ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            return TenantErrors.KeyRequired;
        }

        if (!IsValidKey(normalizedKey))
        {
            return TenantErrors.KeyInvalid;
        }

        return new Tenant(id, name.Trim(), normalizedKey, status);
    }

    public Result<Updated> StartTrial(DateTimeOffset trialEndsAtUtc)
    {
        if (trialEndsAtUtc <= DateTimeOffset.UtcNow)
        {
            return TenantErrors.InvalidSubscriptionPeriod;
        }

        Status = TenantStatus.Trial;
        TrialEndsAtUtc = trialEndsAtUtc;
        CancellationReadOnlyUntilUtc = null;

        return Result.Updated;
    }

    public void Activate()
    {
        Status = TenantStatus.Active;
        TrialEndsAtUtc = null;
        CancellationReadOnlyUntilUtc = null;
    }

    public Result<Updated> Cancel(DateTimeOffset readOnlyUntilUtc)
    {
        if (readOnlyUntilUtc <= DateTimeOffset.UtcNow)
        {
            return TenantErrors.InvalidSubscriptionPeriod;
        }

        Status = TenantStatus.Cancelled;
        CancellationReadOnlyUntilUtc = readOnlyUntilUtc;

        return Result.Updated;
    }

    private static bool IsValidKey(string key)
    {
        if (key.Length is < 3 or > 63 || key[0] == '-' || key[^1] == '-')
        {
            return false;
        }

        return key.All(character => char.IsAsciiLetterOrDigit(character) || character == '-');
    }
}
