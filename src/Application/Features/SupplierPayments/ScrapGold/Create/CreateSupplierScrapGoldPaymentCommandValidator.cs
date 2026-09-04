using Application.SupplierPayments.ScrapGold.Create;
using FluentValidation;

namespace Application.SupplierPayments.ScrapGold.Create;

internal sealed class CreateSupplierScrapGoldPaymentCommandValidator : AbstractValidator<CreateSupplierScrapGoldPaymentCommand>
{
    private static readonly int[] ValidKarats = [18, 21, 24];

    public CreateSupplierScrapGoldPaymentCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Karat).Must(k => ValidKarats.Contains(k)).WithMessage("العيار غير صالح");
        RuleFor(x => x.WeightInGrams).GreaterThan(0).WithMessage("الوزن يجب أن يكون أكبر من صفر");
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}
