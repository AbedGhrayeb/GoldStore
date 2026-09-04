using FluentValidation;

namespace Application.Suppliers.Update;

internal sealed class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(s => s.Id).NotEmpty();
        RuleFor(s => s.Name).NotEmpty().MaximumLength(200);
        RuleFor(s => s.PrimaryPhone).NotEmpty().MaximumLength(10);
        RuleFor(s => s.SecondaryPhone).MaximumLength(10).When(s => s.SecondaryPhone is not null);
        RuleFor(s => s.BankAccountNumber).NotEmpty().MaximumLength(20);
        RuleFor(s => s.Notes).MaximumLength(500).When(s => s.Notes is not null);
    }
}
