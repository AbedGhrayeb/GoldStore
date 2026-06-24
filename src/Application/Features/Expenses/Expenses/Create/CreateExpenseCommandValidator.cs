using Application.Features.Expenses.Expenses.Create;
using FluentValidation;

namespace Application.Features.Expenses.Expenses.Create;

internal sealed class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseCommandValidator()
    {
        RuleFor(x => x.ExpenseDate).NotEmpty().WithMessage("تاريخ المصروف مطلوب");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("المبلغ يجب أن يكون أكبر من صفر");
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("يجب اختيار حساب الدفع");
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}