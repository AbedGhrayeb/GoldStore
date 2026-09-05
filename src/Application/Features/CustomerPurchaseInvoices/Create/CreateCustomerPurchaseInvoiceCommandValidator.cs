// <copyright file="CreateCustomerPurchaseInvoiceCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Sales;
using FluentValidation;

namespace Application.Features.CustomerPurchaseInvoices.Create;

internal sealed class CreateCustomerPurchaseInvoiceCommandValidator : AbstractValidator<CreateCustomerPurchaseInvoiceCommand>
{
    public CreateCustomerPurchaseInvoiceCommandValidator()
    {
        this.RuleFor(x => x.SellerName)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("اسم البائع مطلوب");
        this.RuleFor(x => x.SellerIdNumber)
            .MaximumLength(9).When(x => x.SellerIdNumber is not null)
            .WithMessage("رقم الهوية مطلوب");

        this.RuleFor(x => x.SellerPhone)
            .MaximumLength(10)
            .When(x => x.SellerPhone is not null);

        this.RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(c => c is "JOD" or "USD" or "ILS")
            .WithMessage("العملة غير صالحة");

        this.RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("يجب إضافة صنف واحد على الأقل");

        this.RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Karat)
                .Must(k => k is 18 or 21 or 24)
                .WithMessage("العيار يجب أن يكون 18 أو 21 أو 24");

            item.RuleFor(i => i.WeightInGrams)
                .GreaterThan(0)
                .WithMessage("الوزن يجب أن يكون أكبر من صفر");

            item.RuleFor(i => i.PricePerGram)
                .GreaterThan(0)
                .WithMessage("السعر للجرام يجب أن يكون أكبر من صفر");
        });

        this.RuleFor(x => x.TotalAmount)
            .GreaterThan(0)
            .WithMessage("المبلغ المستحق يجب أن يكون أكبر من صفر");

        this.RuleFor(x => x.AmountPaid)
            .GreaterThanOrEqualTo(0)
            .WithMessage("المبلغ المدفوع يجب أن يكون صفر أو أكثر");

        this.RuleFor(x => x.PaymentMethod)
            .Must(p => Enum.IsDefined(typeof(PaymentMethod), p))
            .WithMessage("طريقة الدفع غير صالحة");

        this.RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("حساب الدفع مطلوب");

        this.RuleFor(x => x.SellerAccountNumber)
            .MaximumLength(20)
            .When(x => x.SellerAccountNumber is not null);

        this.RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => x.Notes is not null);
    }
}
