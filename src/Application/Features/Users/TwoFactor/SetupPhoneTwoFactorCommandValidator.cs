// <copyright file="SetupPhoneTwoFactorCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Features.Users.TwoFactor;

internal sealed class SetupPhoneTwoFactorCommandValidator : AbstractValidator<SetupPhoneTwoFactorCommand>
{
    public SetupPhoneTwoFactorCommandValidator()
    {
        this.RuleFor(x => x.UserId).NotEmpty();
        this.RuleFor(x => x.IdToken).NotEmpty().MinimumLength(10);
    }
}
