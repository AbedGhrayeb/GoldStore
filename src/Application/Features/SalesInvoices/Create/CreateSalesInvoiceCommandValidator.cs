// <copyright file="CreateSalesInvoiceCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Features.SalesInvoices.Create;
using Domain.Common;
using Domain.Sales;
using FluentValidation;

namespace Application.Features.SalesInvoices.Create;

internal sealed class CreateSalesInvoiceCommandValidator : AbstractValidator<CreateSalesInvoiceCommand>
{
    public CreateSalesInvoiceCommandValidator()
    {
        this.RuleFor(x => x.CustomerName)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("اسم العميل مطلوب");

        this.RuleFor(x => x.CustomerPhone)
            .MaximumLength(20)
            .When(x => x.CustomerPhone is not null);

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
            .Must(p => p is null || Enum.IsDefined(typeof(PaymentMethod), p))
            .When(x => x.PaymentMethod.HasValue)
            .WithMessage("طريقة الدفع غير صالحة");

        this.RuleFor(x => x.AccountId)
            .NotEmpty()
            .When(x => x.PaymentMethod == 2 && x.AmountPaid > 0)
            .WithMessage("حساب الاستلام مطلوب عند الدفع البنكي");

        this.RuleFor(x => x.BuyerAccountNumber)
            .NotEmpty()
            .When(x => x.PaymentMethod == 2 && x.AmountPaid > 0)
            .WithMessage("رقم حساب المشتري مطلوب للتحويل البنكي");

        this.RuleFor(x => x.EmployeeId).NotNull()
            .WithMessage("اسم المشتري مطلوب");

        this.RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);

        this.RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("التاريخ مطلوب");
    }
}
