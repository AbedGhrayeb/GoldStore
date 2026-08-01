using Domain.Debts;
using FluentValidation;

namespace Application.Features.Finance.Debts.Create;

internal sealed class CreateDebtCommandValidator : AbstractValidator<CreateDebtCommand>
{
    public CreateDebtCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("الاسم مطلوب");

        RuleFor(x => x.Phone)
            .MaximumLength(10)
            .When(x => x.Phone is not null);

        RuleFor(x => x.Direction)
            .Must(d => Enum.IsDefined(typeof(DebtDirection), d))
            .WithMessage("اتجاه الدين غير صالح");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(c => c is "JOD" or "USD" or "ILS")
            .WithMessage("العملة غير صالحة");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("المبلغ يجب أن يكون أكبر من صفر");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);

        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("الحساب المالي مطلوب");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("التاريخ مطلوب");
    }
}
