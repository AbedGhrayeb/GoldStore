// <copyright file="RegisterUserCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Users.Register;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        this.RuleFor(c => c.FirstName).NotEmpty();
        this.RuleFor(c => c.LastName).NotEmpty();
        this.RuleFor(c => c.Email).NotEmpty().EmailAddress();
        this.RuleFor(c => c.Password).NotEmpty().MinimumLength(8);
    }
}
