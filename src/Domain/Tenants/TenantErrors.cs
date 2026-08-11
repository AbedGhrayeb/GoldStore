using SharedKernel.Result;

namespace Domain.Tenants;

public static class TenantErrors
{
    public static Error IdRequired => Error.Validation("Tenants.Id.Required", "Tenant Id is required.");

    public static Error NameRequired => Error.Validation("Tenants.Name.Required", "Tenant name is required.");

    public static Error DisplayNameRequired => Error.Validation("Tenants.DisplayName.Required", "Store display name is required.");

    public static Error TimeZoneRequired => Error.Validation("Tenants.TimeZone.Required", "Time zone is required.");

    public static Error KeyRequired => Error.Validation("Tenants.Key.Required", "Tenant key is required.");

    public static Error KeyInvalid => Error.Validation("Tenants.Key.Invalid", "Tenant key must contain only lowercase letters, numbers, and hyphens.");

    public static Error InvalidSubscriptionPeriod => Error.Validation("Tenants.SubscriptionPeriod.Invalid", "Subscription end date must be after its start date.");

    public static Error InvalidPlanLimit => Error.Validation("Tenants.PlanLimit.Invalid", "Subscription plan limits cannot be negative.");

    public static Error EmailRequired => Error.Validation("PlatformUsers.Email.Required", "Platform user email is required.");

    public static Error PasswordRequired => Error.Validation("PlatformUsers.Password.Required", "Platform user password is required.");
}
