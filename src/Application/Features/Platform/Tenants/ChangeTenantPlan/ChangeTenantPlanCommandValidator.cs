using FluentValidation;

namespace Application.Features.Platform.Tenants.ChangeTenantPlan;

internal sealed class ChangeTenantPlanCommandValidator : AbstractValidator<ChangeTenantPlanCommand>
{
    public ChangeTenantPlanCommandValidator()
    {
        RuleFor(c => c.NewPlanId).NotEmpty();
        RuleFor(c => c.Interval).IsInEnum();
    }
}
