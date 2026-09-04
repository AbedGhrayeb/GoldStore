using FluentValidation;

namespace Application.Features.Subscriptions.Renew;

internal sealed class RenewTenantSubscriptionCommandValidator : AbstractValidator<RenewTenantSubscriptionCommand>
{
    public RenewTenantSubscriptionCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.BillingCycle).IsInEnum();
        RuleFor(x => x.StartsAtUtc).NotEmpty();
        RuleFor(x => x.EndsAtUtc).NotEmpty().GreaterThan(x => x.StartsAtUtc).WithMessage("تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية");
    }
}
