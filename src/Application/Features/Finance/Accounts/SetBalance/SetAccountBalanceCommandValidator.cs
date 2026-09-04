using Application.Finance.Accounts.SetBalance;
using FluentValidation;

namespace Application.Finance.Accounts.SetBalance;

internal sealed class SetAccountBalanceCommandValidator : AbstractValidator<SetAccountBalanceCommand>
{
    public SetAccountBalanceCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("معرف الحساب مطلوب");
        RuleFor(x => x.TargetBalance).GreaterThanOrEqualTo(0m).WithMessage("الرصيد المستهدف لا يمكن أن يكون سالباً");
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes is not null);
    }
}
