// <copyright file="UpdateUserCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Users.Update;

internal sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        this.RuleFor(c => c.Id).NotEmpty();
        this.RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        this.RuleFor(c => c.LastName).NotEmpty().MaximumLength(100);
        this.RuleFor(c => c.Password).MinimumLength(8).MaximumLength(100).When(c => !string.IsNullOrEmpty(c.Password));
        this.RuleFor(c => c.PhoneNumber).MaximumLength(30).Matches(@"^[0-9+\-\s\(\)]*$").When(c => !string.IsNullOrWhiteSpace(c.PhoneNumber)).WithMessage("رقم الهاتف غير صالح");
        this.RuleFor(c => c.WhatsappNumber).MaximumLength(30).Matches(@"^[0-9+\-\s\(\)]*$").When(c => !string.IsNullOrWhiteSpace(c.WhatsappNumber)).WithMessage("رقم الواتساب غير صالح");
    }
}
