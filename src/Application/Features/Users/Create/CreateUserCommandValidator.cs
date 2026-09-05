// <copyright file="CreateUserCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Users.Create;

internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        this.RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        this.RuleFor(c => c.LastName).NotEmpty().MaximumLength(100);
        this.RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
        this.RuleFor(c => c.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        this.RuleFor(c => c.PhoneNumber).MaximumLength(30).Matches(@"^[0-9+\-\s\(\)]*$").When(c => !string.IsNullOrWhiteSpace(c.PhoneNumber)).WithMessage("رقم الهاتف غير صالح");
        this.RuleFor(c => c.WhatsappNumber).MaximumLength(30).Matches(@"^[0-9+\-\s\(\)]*$").When(c => !string.IsNullOrWhiteSpace(c.WhatsappNumber)).WithMessage("رقم الواتساب غير صالح");
    }
}
