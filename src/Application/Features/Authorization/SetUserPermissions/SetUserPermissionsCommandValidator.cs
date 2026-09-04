using FluentValidation;

namespace Application.Authorization.SetUserPermissions;

internal sealed class SetUserPermissionsCommandValidator : AbstractValidator<SetUserPermissionsCommand>
{
    public SetUserPermissionsCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.PermissionKeys).NotNull();
    }
}
