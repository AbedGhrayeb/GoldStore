using Domain.Suppliers;
using FluentValidation;

namespace Application.Features.SupplierFinancialTransactions.Create;

internal sealed class CreateSupplierFinancialTransactionCommandValidator : AbstractValidator<CreateSupplierFinancialTransactionCommand>
{
    public CreateSupplierFinancialTransactionCommandValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty()
            .WithMessage("المورد مطلوب");

        RuleFor(x => x.Direction)
            .Must(d => Enum.IsDefined(typeof(SupplierFinancialTransactionDirection), d))
            .WithMessage("اتجاه المعاملة غير صالح");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("المبلغ يجب أن يكون أكبر من صفر");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(currency => Enum.TryParse(currency, ignoreCase: true, out Domain.Common.Currency parsed)
                && Enum.IsDefined(parsed))
            .WithMessage("العملة غير صالحة");

        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("الحساب المالي مطلوب");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("التاريخ مطلوب");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);
    }
}
