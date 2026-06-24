using FluentValidation;

namespace Application.Suppliers.Create;

internal sealed class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(s => s.Name).NotEmpty().MaximumLength(200);
        RuleFor(s => s.PrimaryPhone).NotEmpty().MaximumLength(30);
        RuleFor(s => s.SecondaryPhone).MaximumLength(30).When(s => s.SecondaryPhone is not null);
        RuleFor(s => s.BankAccountNumber).NotEmpty().MaximumLength(100);
        RuleFor(s => s.Notes).MaximumLength(1000).When(s => s.Notes is not null);
    }
}