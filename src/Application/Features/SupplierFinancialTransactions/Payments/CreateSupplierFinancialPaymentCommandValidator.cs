using FluentValidation;

namespace Application.Features.SupplierFinancialTransactions.Payments;

internal sealed class CreateSupplierFinancialPaymentCommandValidator : AbstractValidator<CreateSupplierFinancialPaymentCommand>
{
    public CreateSupplierFinancialPaymentCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("المعاملة المالية مطلوبة");

        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("الحساب المالي مطلوب");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("المبلغ يجب أن يكون أكبر من صفر");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("التاريخ مطلوب");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);
    }
}
