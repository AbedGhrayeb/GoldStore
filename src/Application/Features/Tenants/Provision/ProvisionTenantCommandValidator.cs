using FluentValidation;

namespace Application.Tenants.Provision;

internal sealed class ProvisionTenantCommandValidator : AbstractValidator<ProvisionTenantCommand>
{
    public ProvisionTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Key).NotEmpty().MaximumLength(63)
            .Matches("^[a-z0-9]([a-z0-9-]*[a-z0-9])?$").WithMessage("المعرف يجب أن يحتوي على أحرف وأرقام صغيرة وشرطات فقط");
        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.AdminFirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminLastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(6);
        RuleFor(x => x.SubscriptionPlanId).NotEmpty();
        RuleFor(x => x.StartsAtUtc).NotEmpty();
        RuleFor(x => x.EndsAtUtc).GreaterThan(x => x.StartsAtUtc);
    }
}
