// <copyright file="UpdateExpenseCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Features.Expenses.Expenses.Update;
using FluentValidation;

namespace Application.Features.Expenses.Expenses.Update;

internal sealed class UpdateExpenseCommandValidator : AbstractValidator<UpdateExpenseCommand>
{
    public UpdateExpenseCommandValidator()
    {
        this.RuleFor(x => x.Id).NotEmpty();
        this.RuleFor(x => x.ExpenseDate).NotEmpty().WithMessage("تاريخ المصروف مطلوب");
        this.RuleFor(x => x.Amount).GreaterThan(0).WithMessage("المبلغ يجب أن يكون أكبر من صفر");
        this.RuleFor(x => x.AccountId).NotEmpty().WithMessage("يجب اختيار حساب الدفع");
        this.RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
