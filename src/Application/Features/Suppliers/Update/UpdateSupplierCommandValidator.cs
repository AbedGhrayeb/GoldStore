// <copyright file="UpdateSupplierCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Suppliers.Update;

internal sealed class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        this.RuleFor(s => s.Id).NotEmpty();
        this.RuleFor(s => s.Name).NotEmpty().MaximumLength(200);
        this.RuleFor(s => s.PrimaryPhone).NotEmpty().MaximumLength(10);
        this.RuleFor(s => s.SecondaryPhone).MaximumLength(10).When(s => s.SecondaryPhone is not null);
        this.RuleFor(s => s.BankAccountNumber).NotEmpty().MaximumLength(20);
        this.RuleFor(s => s.Notes).MaximumLength(500).When(s => s.Notes is not null);
    }
}
