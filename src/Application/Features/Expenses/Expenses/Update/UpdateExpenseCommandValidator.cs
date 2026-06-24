using Application.Features.Expenses.Expenses.Update;
using FluentValidation;

namespace Application.Features.Expenses.Expenses.Update;

internal sealed class UpdateExpenseCommandValidator : AbstractValidator<UpdateExpenseCommand>
{
    public UpdateExpenseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ExpenseDate).NotEmpty().WithMessage("تاريخ المصروف مطلوب");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("المبلغ يجب أن يكون أكبر من صفر");
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("يجب اختيار حساب الدفع");
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}