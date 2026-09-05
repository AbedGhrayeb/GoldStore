// <copyright file="CreateSupplierDeliveryCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.SupplierDeliveries.Create;
using FluentValidation;

namespace Application.SupplierDeliveries.Create;

internal sealed class CreateSupplierDeliveryCommandValidator : AbstractValidator<CreateSupplierDeliveryCommand>
{
    private static readonly int[] ValidKarats = [18, 21, 24];
    private static readonly string[] ValidCurrencies = ["Jod", "Usd", "Ils", "JOD", "USD", "ILS"];

    public CreateSupplierDeliveryCommandValidator()
    {
        this.RuleFor(x => x.SupplierId).NotEmpty();
        this.RuleFor(x => x.Lines).NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");
        this.RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Karat).Must(k => ValidKarats.Contains(k)).WithMessage("العيار غير صالح");
            line.RuleFor(l => l.WeightInGrams).GreaterThan(0).WithMessage("الوزن يجب أن يكون أكبر من صفر");
        });
        this.RuleFor(x => x.ManufacturingFeePerGram).GreaterThanOrEqualTo(0).WithMessage("أجور التصنيع لا يمكن أن تكون سالبة");
        this.RuleFor(x => x.ManufacturingFeeCurrency).Must(c => ValidCurrencies.Contains(c, System.StringComparer.OrdinalIgnoreCase)).WithMessage("العملة غير صالحة");
        this.RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}
