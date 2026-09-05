// <copyright file="SetUserRolesCommandValidator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;

namespace Application.Authorization.SetUserRoles;

internal sealed class SetUserRolesCommandValidator : AbstractValidator<SetUserRolesCommand>
{
    public SetUserRolesCommandValidator()
    {
        this.RuleFor(c => c.UserId).NotEmpty();
        this.RuleFor(c => c.RoleIds).NotNull();
        this.RuleForEach(c => c.RoleIds).NotEmpty();
    }
}
