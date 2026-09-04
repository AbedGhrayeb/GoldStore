using FluentValidation;

namespace Application.Features.TenantSubscription.RenewSubscription;

internal sealed class RenewSubscriptionCommandValidator : AbstractValidator<RenewSubscriptionCommand>
{
    public RenewSubscriptionCommandValidator()
    {
        RuleFor(c => c.Interval).IsInEnum();
    }
}
