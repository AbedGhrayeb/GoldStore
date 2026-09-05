// <copyright file="CreateSupplierFinancialPaymentCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Features.SupplierFinancialTransactions.Payments;

internal sealed class CreateSupplierFinancialPaymentCommandValidator : AbstractValidator<CreateSupplierFinancialPaymentCommand>
{
    public CreateSupplierFinancialPaymentCommandValidator()
    {
        this.RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("المعاملة المالية مطلوبة");

        this.RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("الحساب المالي مطلوب");

        this.RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("المبلغ يجب أن يكون أكبر من صفر");

        this.RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("التاريخ مطلوب");

        this.RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);
    }
}
