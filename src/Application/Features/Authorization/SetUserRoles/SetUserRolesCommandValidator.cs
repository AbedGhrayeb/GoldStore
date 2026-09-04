using FluentValidation;

namespace Application.Authorization.SetUserRoles;

internal sealed class SetUserRolesCommandValidator : AbstractValidator<SetUserRolesCommand>
{
    public SetUserRolesCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.RoleIds).NotNull();
        RuleForEach(c => c.RoleIds).NotEmpty();
    }
}
