using FluentValidation;

namespace Application.Features.Finance.Debts.Update;

internal sealed class UpdateDebtCommandValidator : AbstractValidator<UpdateDebtCommand>
{
    public UpdateDebtCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("المعرف مطلوب");

        RuleFor(x => x.Name)
            .MaximumLength(200)
            .When(x => x.Name is not null);

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .When(x => x.Phone is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);

        RuleFor(x => x.NewAmount)
            .GreaterThan(0)
            .When(x => x.NewAmount.HasValue)
            .WithMessage("المبلغ الجديد يجب أن يكون أكبر من صفر");

        RuleFor(x => x.NewAccountId)
            .NotEmpty()
            .When(x => x.NewAccountId.HasValue)
            .WithMessage("الحساب المالي الجديد غير صالح");
    }
}
