using Application.Features.SalesInvoices.Create;
using Domain.Common;
using Domain.Sales;
using FluentValidation;

namespace Application.Features.SalesInvoices.Create;

internal sealed class CreateSalesInvoiceCommandValidator : AbstractValidator<CreateSalesInvoiceCommand>
{
    public CreateSalesInvoiceCommandValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("اسم العميل مطلوب");

        RuleFor(x => x.CustomerPhone)
            .MaximumLength(20)
            .When(x => x.CustomerPhone is not null);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(c => c is "JOD" or "USD" or "ILS")
            .WithMessage("العملة غير صالحة");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("يجب إضافة صنف واحد على الأقل");

        RuleForEach(x => x.Items).ChildRules(item =>
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

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0)
            .WithMessage("المبلغ المستحق يجب أن يكون أكبر من صفر");

        RuleFor(x => x.AmountPaid)
            .GreaterThanOrEqualTo(0)
            .WithMessage("المبلغ المدفوع يجب أن يكون صفر أو أكثر");

        RuleFor(x => x.PaymentMethod)
            .Must(p => p is null || Enum.IsDefined(typeof(PaymentMethod), p))
            .When(x => x.PaymentMethod.HasValue)
            .WithMessage("طريقة الدفع غير صالحة");

        RuleFor(x => x.AccountId)
            .NotEmpty()
            .When(x => x.PaymentMethod == 2 && x.AmountPaid > 0)
            .WithMessage("حساب الاستلام مطلوب عند الدفع البنكي");

        RuleFor(x => x.BuyerAccountNumber)
            .NotEmpty()
            .When(x => x.PaymentMethod == 2 && x.AmountPaid > 0)
            .WithMessage("رقم حساب المشتري مطلوب للتحويل البنكي");

        RuleFor(x => x.EmployeeId).NotNull()
            .WithMessage("اسم المشتري مطلوب");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("التاريخ مطلوب");
    }
}
