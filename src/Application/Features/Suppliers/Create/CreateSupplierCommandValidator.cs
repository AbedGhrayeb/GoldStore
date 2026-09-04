using FluentValidation;

namespace Application.Suppliers.Create;

internal sealed class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(s => s.Name).NotEmpty().MaximumLength(200);
        RuleFor(s => s.PrimaryPhone).NotEmpty().MaximumLength(10);
        RuleFor(s => s.SecondaryPhone).MaximumLength(10).When(s => s.SecondaryPhone is not null);
        RuleFor(s => s.BankAccountNumber).NotEmpty().MaximumLength(20);
        RuleFor(s => s.Notes).MaximumLength(500).When(s => s.Notes is not null);
    }
}
