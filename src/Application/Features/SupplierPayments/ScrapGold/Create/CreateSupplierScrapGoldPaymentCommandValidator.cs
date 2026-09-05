// <copyright file="CreateSupplierScrapGoldPaymentCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.SupplierPayments.ScrapGold.Create;
using FluentValidation;

namespace Application.SupplierPayments.ScrapGold.Create;

internal sealed class CreateSupplierScrapGoldPaymentCommandValidator : AbstractValidator<CreateSupplierScrapGoldPaymentCommand>
{
    private static readonly int[] ValidKarats = [18, 21, 24];

    public CreateSupplierScrapGoldPaymentCommandValidator()
    {
        this.RuleFor(x => x.SupplierId).NotEmpty();
        this.RuleFor(x => x.Karat).Must(k => ValidKarats.Contains(k)).WithMessage("العيار غير صالح");
        this.RuleFor(x => x.WeightInGrams).GreaterThan(0).WithMessage("الوزن يجب أن يكون أكبر من صفر");
        this.RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}
