using Application.SupplierPayments.Manufacturing.Create;
using FluentValidation;

namespace Application.SupplierPayments.Manufacturing.Create;

internal sealed class CreateSupplierManufacturingPaymentCommandValidator : AbstractValidator<CreateSupplierManufacturingPaymentCommand>
{
    private static readonly string[] ValidCurrencies = ["Jod", "Usd", "Ils", "JOD", "USD", "ILS"];

    public CreateSupplierManufacturingPaymentCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("يجب اختيار حساب الدفع");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("المبلغ يجب أن يكون أكبر من صفر");
        RuleFor(x => x.Currency).Must(c => ValidCurrencies.Contains(c, System.StringComparer.OrdinalIgnoreCase)).WithMessage("العملة غير صالحة");
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}
