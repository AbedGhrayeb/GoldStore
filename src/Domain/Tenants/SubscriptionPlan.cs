using SharedKernel;
using SharedKernel.Result;

namespace Domain.Tenants;

public sealed class SubscriptionPlan : Entity
{
    public string Name { get; private set; }

    public string Key { get; private set; }

    public int? MaximumActiveUsers { get; private set; }

    public int? MaximumPostedInvoicesPerPeriod { get; private set; }

    public int? MaximumActiveBranches { get; private set; }

    public long? MaximumStorageBytes { get; private set; }

    private SubscriptionPlan()
    {
        Name = string.Empty;
        Key = string.Empty;
    }

    private SubscriptionPlan(Guid id, string name, string key, int? maximumActiveUsers, int? maximumPostedInvoicesPerPeriod, int? maximumActiveBranches, long? maximumStorageBytes) : base(id)
    {
        Name = name;
        Key = key;
        MaximumActiveUsers = maximumActiveUsers;
        MaximumPostedInvoicesPerPeriod = maximumPostedInvoicesPerPeriod;
        MaximumActiveBranches = maximumActiveBranches;
        MaximumStorageBytes = maximumStorageBytes;
        IsActive = true;
    }

    public static Result<SubscriptionPlan> Create(string name, string key, int? maximumActiveUsers, int? maximumPostedInvoicesPerPeriod, int? maximumActiveBranches, long? maximumStorageBytes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return TenantErrors.NameRequired;
        }

        string normalizedKey = key.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            return TenantErrors.KeyRequired;
        }

        if (maximumActiveUsers < 0 || maximumPostedInvoicesPerPeriod < 0 || maximumActiveBranches < 0 || maximumStorageBytes < 0)
        {
            return TenantErrors.InvalidPlanLimit;
        }

        return new SubscriptionPlan(Guid.CreateVersion7(), name.Trim(), normalizedKey, maximumActiveUsers, maximumPostedInvoicesPerPeriod, maximumActiveBranches, maximumStorageBytes);
    }
}
