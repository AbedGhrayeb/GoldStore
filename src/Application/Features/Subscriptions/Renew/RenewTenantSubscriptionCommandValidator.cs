// <copyright file="RenewTenantSubscriptionCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Features.Subscriptions.Renew;

internal sealed class RenewTenantSubscriptionCommandValidator : AbstractValidator<RenewTenantSubscriptionCommand>
{
    public RenewTenantSubscriptionCommandValidator()
    {
        this.RuleFor(x => x.TenantId).NotEmpty();
        this.RuleFor(x => x.BillingCycle).IsInEnum();
        this.RuleFor(x => x.StartsAtUtc).NotEmpty();
        this.RuleFor(x => x.EndsAtUtc).NotEmpty().GreaterThan(x => x.StartsAtUtc).WithMessage("تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية");
    }
}
