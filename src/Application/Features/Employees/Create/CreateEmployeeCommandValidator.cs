// <copyright file="CreateEmployeeCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Employees.Create;

internal sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        this.RuleFor(c => c.FirstName).NotEmpty().MaximumLength(20);
        this.RuleFor(c => c.LastName).NotEmpty().MaximumLength(20);
        this.RuleFor(c => c.Role).IsInEnum();
        this.RuleFor(c => c.Salary).GreaterThan(0);
        this.RuleFor(c => c.Currency).IsInEnum();
        this.RuleFor(c => c.SalaryCycle).IsInEnum();

        this.When(c => c.ConnectToUser && !c.ExistingUserId.HasValue, () =>
        {
            this.RuleFor(c => c.NewUserEmail).NotEmpty().EmailAddress().MaximumLength(256);
            this.RuleFor(c => c.NewUserPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
        });
    }
}
