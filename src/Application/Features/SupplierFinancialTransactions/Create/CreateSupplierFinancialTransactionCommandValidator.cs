// <copyright file="CreateSupplierFinancialTransactionCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Suppliers;
using FluentValidation;

namespace Application.Features.SupplierFinancialTransactions.Create;

internal sealed class CreateSupplierFinancialTransactionCommandValidator : AbstractValidator<CreateSupplierFinancialTransactionCommand>
{
    public CreateSupplierFinancialTransactionCommandValidator()
    {
        this.RuleFor(x => x.SupplierId)
            .NotEmpty()
            .WithMessage("المورد مطلوب");

        this.RuleFor(x => x.Direction)
            .Must(d => Enum.IsDefined(typeof(SupplierFinancialTransactionDirection), d))
            .WithMessage("اتجاه المعاملة غير صالح");

        this.RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("المبلغ يجب أن يكون أكبر من صفر");

        this.RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(currency => Enum.TryParse(currency, ignoreCase: true, out Domain.Common.Currency parsed)
                && Enum.IsDefined(parsed))
            .WithMessage("العملة غير صالحة");

        this.RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("الحساب المالي مطلوب");

        this.RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("التاريخ مطلوب");

        this.RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);
    }
}
