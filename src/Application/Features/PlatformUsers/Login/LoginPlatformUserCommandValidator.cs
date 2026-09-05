// <copyright file="LoginPlatformUserCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.PlatformUsers.Login;
using FluentValidation;

namespace Application.Features.PlatformUsers.Login;

public class LoginPlatformUserCommandValidator : AbstractValidator<LoginPlatformUserCommand>
{
    public LoginPlatformUserCommandValidator()
    {
        this.RuleFor(c => c.Email).NotEmpty().EmailAddress();
        this.RuleFor(c => c.Password).NotEmpty().MinimumLength(8).MaximumLength(20);
    }
}
