using Application.Finance.Accounts.Create;
using FluentValidation;

namespace Application.Finance.Accounts.Create;

internal sealed class CreateFinancialAccountCommandValidator : AbstractValidator<CreateFinancialAccountCommand>
{
    private static readonly string[] ValidCurrencies = ["JOD", "USD", "ILS"];

    public CreateFinancialAccountCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("اسم الحساب مطلوب").MaximumLength(200);
        RuleFor(x => x.Currency).Must(c => ValidCurrencies.Contains(c, System.StringComparer.OrdinalIgnoreCase)).WithMessage("العملة غير صالحة");
        RuleFor(x => x.AccountNumber).MaximumLength(50).When(x => x.AccountNumber is not null);
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes is not null);
        RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0m).WithMessage("الرصيد الافتتاحي لا يمكن أن يكون سالباً");
    }
}
