// <copyright file="UpdateEmployeeCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Employees.Update;

internal sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        this.RuleFor(c => c.Id).NotEmpty();
        this.RuleFor(c => c.FirstName).NotEmpty().MaximumLength(20);
        this.RuleFor(c => c.LastName).NotEmpty().MaximumLength(20);
        this.RuleFor(c => c.Role).IsInEnum();
        this.RuleFor(c => c.Salary).GreaterThan(0);
        this.RuleFor(c => c.Currency).IsInEnum();
        this.RuleFor(c => c.SalaryCycle).IsInEnum();
    }
}
