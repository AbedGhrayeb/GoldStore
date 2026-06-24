using FluentValidation;

namespace Application.Suppliers.Update;

internal sealed class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(s => s.Id).NotEmpty();
        RuleFor(s => s.Name).NotEmpty().MaximumLength(200);
        RuleFor(s => s.PrimaryPhone).NotEmpty().MaximumLength(30);
        RuleFor(s => s.SecondaryPhone).MaximumLength(30).When(s => s.SecondaryPhone is not null);
        RuleFor(s => s.BankAccountNumber).NotEmpty().MaximumLength(100);
        RuleFor(s => s.Notes).MaximumLength(1000).When(s => s.Notes is not null);
    }
}