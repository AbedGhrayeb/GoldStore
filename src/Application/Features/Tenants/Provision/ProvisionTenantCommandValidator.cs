// <copyright file="ProvisionTenantCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Tenants.Provision;

internal sealed class ProvisionTenantCommandValidator : AbstractValidator<ProvisionTenantCommand>
{
    public ProvisionTenantCommandValidator()
    {
        this.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        this.RuleFor(x => x.Key).NotEmpty().MaximumLength(63)
            .Matches("^[a-z0-9]([a-z0-9-]*[a-z0-9])?$").WithMessage("المعرف يجب أن يحتوي على أحرف وأرقام صغيرة وشرطات فقط");
        this.RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(64);
        this.RuleFor(x => x.AdminFirstName).NotEmpty().MaximumLength(100);
        this.RuleFor(x => x.AdminLastName).NotEmpty().MaximumLength(100);
        this.RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        this.RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(6);
        this.RuleFor(x => x.AdminPhoneNumber).MaximumLength(30).Matches(@"^[0-9+\-\s\(\)]*$").When(x => !string.IsNullOrWhiteSpace(x.AdminPhoneNumber)).WithMessage("رقم الهاتف غير صالح");
        this.RuleFor(x => x.AdminWhatsappNumber).MaximumLength(30).Matches(@"^[0-9+\-\s\(\)]*$").When(x => !string.IsNullOrWhiteSpace(x.AdminWhatsappNumber)).WithMessage("رقم الواتساب غير صالح");
        this.RuleFor(x => x.SubscriptionPlanId).NotEmpty();
        this.RuleFor(x => x.StartsAtUtc).NotEmpty();
        this.RuleFor(x => x.EndsAtUtc).GreaterThan(x => x.StartsAtUtc).When(x => x.EndsAtUtc != default);
    }
}
