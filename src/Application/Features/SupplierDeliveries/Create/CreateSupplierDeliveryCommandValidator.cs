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
        this.RuleFor(x => x.ManufacturingFeeCurrency).Must(c => c.Equals("JOD", System.StringComparison.OrdinalIgnoreCase)).WithMessage("عملة أجور التصنيع يجب أن تكون بالدينار (JOD)");
        this.RuleFor(x => x.AmountDue).GreaterThanOrEqualTo(0).WithMessage("المبلغ المستحق لا يمكن أن يكون سالباً");
        this.RuleFor(x => x.AmountDueCurrency).Must(c => ValidCurrencies.Contains(c, System.StringComparer.OrdinalIgnoreCase)).WithMessage("عملة المبلغ المستحق غير صالحة");
        this.RuleForEach(x => x.PaymentLegs!).ChildRules(leg =>
        {
            leg.RuleFor(l => l.Amount).GreaterThan(0).WithMessage("مبلغ الدفعة يجب أن يكون أكبر من صفر");
            leg.RuleFor(l => l.Currency).Must(c => ValidCurrencies.Contains(c, System.StringComparer.OrdinalIgnoreCase)).WithMessage("عملة الدفعة غير صالحة");
        }).When(x => x.PaymentLegs is not null);

        // The rate is only meaningful for cross-currency legs (same-currency legs are
        // normalized to 1 by the payment processor, like the invoice dialogs do).
        this.RuleFor(x => x.PaymentLegs).Custom((legs, context) =>
        {
            if (legs is null)
            {
                return;
            }

            string dueCurrency = context.InstanceToValidate.AmountDueCurrency ?? string.Empty;
            for (int i = 0; i < legs.Count; i++)
            {
                var leg = legs[i];
                if (leg is null)
                {
                    context.AddFailure($"PaymentLegs[{i}]", "بند الدفعة غير صالح");
                }
                else if (!leg.Currency.Equals(dueCurrency, System.StringComparison.OrdinalIgnoreCase)
                    && leg.ExchangeRate <= 0)
                {
                    context.AddFailure($"PaymentLegs[{i}].ExchangeRate", "سعر الصرف يجب أن يكون أكبر من صفر للدفعات بعملة مختلفة");
                }
            }
        }).When(x => x.PaymentLegs is not null);
        this.RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}
