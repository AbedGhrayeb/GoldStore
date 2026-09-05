// <copyright file="ResetPasswordWithPhoneCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Features.Users.TwoFactor;

internal sealed class ResetPasswordWithPhoneCommandValidator : AbstractValidator<ResetPasswordWithPhoneCommand>
{
    public ResetPasswordWithPhoneCommandValidator()
    {
        this.RuleFor(x => x.EmailOrPhone).NotEmpty();
        this.RuleFor(x => x.IdToken).NotEmpty().MinimumLength(10);
        this.RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}
