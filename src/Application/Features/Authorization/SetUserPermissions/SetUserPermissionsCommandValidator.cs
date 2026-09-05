// <copyright file="SetUserPermissionsCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Authorization.SetUserPermissions;

internal sealed class SetUserPermissionsCommandValidator : AbstractValidator<SetUserPermissionsCommand>
{
    public SetUserPermissionsCommandValidator()
    {
        this.RuleFor(c => c.UserId).NotEmpty();
        this.RuleFor(c => c.PermissionKeys).NotNull();
    }
}
