// <copyright file="CreateSupplierManufacturingPaymentCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.SupplierPayments.Manufacturing.Create;
using FluentValidation;

namespace Application.SupplierPayments.Manufacturing.Create;

internal sealed class CreateSupplierManufacturingPaymentCommandValidator : AbstractValidator<CreateSupplierManufacturingPaymentCommand>
{
    private static readonly string[] ValidCurrencies = ["Jod", "Usd", "Ils", "JOD", "USD", "ILS"];

    public CreateSupplierManufacturingPaymentCommandValidator()
    {
        this.RuleFor(x => x.SupplierId).NotEmpty();
        this.RuleFor(x => x.AccountId).NotEmpty().WithMessage("يجب اختيار حساب الدفع");
        this.RuleFor(x => x.Amount).GreaterThan(0).WithMessage("المبلغ يجب أن يكون أكبر من صفر");
        this.RuleFor(x => x.Currency).Must(c => ValidCurrencies.Contains(c, System.StringComparer.OrdinalIgnoreCase)).WithMessage("العملة غير صالحة");
        this.RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}
