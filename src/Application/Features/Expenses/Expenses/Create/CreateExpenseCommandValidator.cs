// <copyright file="CreateExpenseCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Features.Expenses.Expenses.Create;

internal sealed class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseCommandValidator()
    {
        this.RuleFor(x => x.ExpenseDate).NotEmpty().WithMessage("تاريخ المصروف مطلوب");
        this.RuleFor(x => x.Amount).GreaterThan(0).WithMessage("المبلغ يجب أن يكون أكبر من صفر");
        this.RuleFor(x => x.AccountId).NotEmpty().WithMessage("يجب اختيار حساب الدفع");
        this.RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
