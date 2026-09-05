// <copyright file="PaySalaryCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Employees.PaySalary;

internal sealed class PaySalaryCommandValidator : AbstractValidator<PaySalaryCommand>
{
    public PaySalaryCommandValidator()
    {
        this.RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("معرف الموظف مطلوب");
        this.RuleFor(x => x.AccountId).NotEmpty().WithMessage("يجب اختيار حساب الدفع");
        this.RuleFor(x => x.Amount).GreaterThan(0).WithMessage("مبلغ الدفعة يجب أن يكون قيمة موجبة");
        this.RuleFor(x => x.PaymentDate).NotEmpty().WithMessage("تاريخ الدفع مطلوب");
        this.RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes is not null);
    }
}
