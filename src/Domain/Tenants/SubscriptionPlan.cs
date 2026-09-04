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

    public bool IsTrial { get; private set; }

    /// <summary>Duration in months: 1,3,6,12,24 etc. For trial fixed to 1.</summary>
    public int DurationInMonths { get; private set; }

    public decimal Price { get; private set; }

    public decimal? DiscountPercent { get; private set; }

    private SubscriptionPlan()
    {
        Name = string.Empty;
        Key = string.Empty;
    }

    private SubscriptionPlan(Guid id, string name, string key, int? maximumActiveUsers, int? maximumPostedInvoicesPerPeriod, int? maximumActiveBranches, long? maximumStorageBytes, bool isTrial, int durationInMonths, decimal price, decimal? discountPercent) : base(id)
    {
        Name = name;
        Key = key;
        MaximumActiveUsers = maximumActiveUsers;
        MaximumPostedInvoicesPerPeriod = maximumPostedInvoicesPerPeriod;
        MaximumActiveBranches = maximumActiveBranches;
        MaximumStorageBytes = maximumStorageBytes;
        IsTrial = isTrial;
        DurationInMonths = durationInMonths;
        Price = price;
        DiscountPercent = discountPercent;
        IsActive = true;
    }

    public static Result<SubscriptionPlan> Create(string name, string? key, int? maximumActiveUsers, int? maximumPostedInvoicesPerPeriod, int? maximumActiveBranches, long? maximumStorageBytes, bool isTrial, int durationInMonths, decimal price, decimal? discountPercent)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return TenantErrors.NameRequired;
        }

        string normalizedKey = string.IsNullOrWhiteSpace(key) ? GenerateKey(name) : key.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            return TenantErrors.KeyRequired;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedKey, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            return TenantErrors.KeyInvalid;
        }

        if (maximumActiveUsers < 0 || maximumPostedInvoicesPerPeriod < 0 || maximumActiveBranches < 0 || maximumStorageBytes < 0)
        {
            return TenantErrors.InvalidPlanLimit;
        }

        if (durationInMonths <= 0 || durationInMonths > 60)
        {
            return TenantErrors.InvalidPlanDuration;
        }

        if (isTrial && durationInMonths != 1)
        {
            return TenantErrors.InvalidTrialDuration;
        }

        if (price < 0)
        {
            return TenantErrors.InvalidPlanPrice;
        }

        if (discountPercent is not null && (discountPercent < 0 || discountPercent > 100))
        {
            return TenantErrors.InvalidDiscountPercent;
        }

        if (isTrial && price != 0)
        {
            // Trial should be free; allow but normalize to 0
            price = 0;
            discountPercent = null;
        }

        return new SubscriptionPlan(Guid.CreateVersion7(), name.Trim(), normalizedKey, maximumActiveUsers, maximumPostedInvoicesPerPeriod, maximumActiveBranches, maximumStorageBytes, isTrial, durationInMonths, price, discountPercent);
    }

    public Result<Updated> Update(string name, string? key, int? maximumActiveUsers, int? maximumPostedInvoicesPerPeriod, int? maximumActiveBranches, long? maximumStorageBytes, bool isTrial, int durationInMonths, decimal price, decimal? discountPercent, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return TenantErrors.NameRequired;
        }

        string normalizedKey = string.IsNullOrWhiteSpace(key) ? GenerateKey(name) : key.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            return TenantErrors.KeyRequired;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedKey, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            return TenantErrors.KeyInvalid;
        }

        if (maximumActiveUsers < 0 || maximumPostedInvoicesPerPeriod < 0 || maximumActiveBranches < 0 || maximumStorageBytes < 0)
        {
            return TenantErrors.InvalidPlanLimit;
        }

        if (durationInMonths <= 0 || durationInMonths > 60)
        {
            return TenantErrors.InvalidPlanDuration;
        }

        if (isTrial && durationInMonths != 1)
        {
            return TenantErrors.InvalidTrialDuration;
        }

        if (price < 0)
        {
            return TenantErrors.InvalidPlanPrice;
        }

        if (discountPercent is not null && (discountPercent < 0 || discountPercent > 100))
        {
            return TenantErrors.InvalidDiscountPercent;
        }

        if (isTrial && price != 0)
        {
            price = 0;
            discountPercent = null;
        }

        Name = name.Trim();
        Key = normalizedKey;
        MaximumActiveUsers = maximumActiveUsers;
        MaximumPostedInvoicesPerPeriod = maximumPostedInvoicesPerPeriod;
        MaximumActiveBranches = maximumActiveBranches;
        MaximumStorageBytes = maximumStorageBytes;
        IsTrial = isTrial;
        DurationInMonths = durationInMonths;
        Price = price;
        DiscountPercent = discountPercent;
        IsActive = isActive;

        return Result.Updated;
    }

    public decimal EffectivePrice => DiscountPercent is null ? Price : Math.Round(Price * (1 - DiscountPercent.Value / 100m), 2);

    public static string GenerateKey(string name)
    {
        string slug = name.Trim().ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = $"plan-{Guid.CreateVersion7():N}"[..8];
        }
        if (slug.Length > 50)
        {
            slug = slug[..50].Trim('-');
        }

        return slug;
    }
}
