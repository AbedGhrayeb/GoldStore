using FluentValidation;

namespace Application.Features.Platform.Tenants.ProvisionTenant;

internal sealed class ProvisionTenantCommandValidator : AbstractValidator<ProvisionTenantCommand>
{
    public ProvisionTenantCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Subdomain)
            .NotEmpty()
            .MaximumLength(63)
            .Matches("^[a-z0-9]([a-z0-9-]{0,28}[a-z0-9])?$");
        RuleFor(c => c.PlanId).NotEmpty();
        RuleFor(c => c.AdminEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c => c.AdminPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(c => c.AdminFirstName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.AdminLastName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Interval).IsInEnum();
    }
}
