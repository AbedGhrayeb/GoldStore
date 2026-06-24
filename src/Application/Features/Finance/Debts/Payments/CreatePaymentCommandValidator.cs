using FluentValidation;

namespace Application.Features.Finance.Debts.Payments;

internal sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.DebtId)
            .NotEmpty()
            .WithMessage("معرف الدين مطلوب");

        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("الحساب مطلوب");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("المبلغ يجب أن يكون أكبر من صفر");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => x.Notes is not null);

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("التاريخ مطلوب");
    }
}
