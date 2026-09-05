// <copyright file="UpdateTenantStatusCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Tenants.UpdateStatus;

internal sealed class UpdateTenantStatusCommandValidator : AbstractValidator<UpdateTenantStatusCommand>
{
    public UpdateTenantStatusCommandValidator()
    {
        this.RuleFor(x => x.TenantId).NotEmpty();
        this.RuleFor(x => x.NewStatus).IsInEnum();
    }
}
