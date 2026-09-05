// <copyright file="LoginUserCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Users.Login;
using FluentValidation;

namespace Application.Features.Users.Login;

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        this.RuleFor(c => c.Email).NotEmpty().EmailAddress();
        this.RuleFor(c => c.Password).NotEmpty().MinimumLength(8).MaximumLength(20);
    }
}
