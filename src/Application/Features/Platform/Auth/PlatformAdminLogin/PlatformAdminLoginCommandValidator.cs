using FluentValidation;

namespace Application.Features.Platform.Auth.PlatformAdminLogin;

internal sealed class PlatformAdminLoginCommandValidator : AbstractValidator<PlatformAdminLoginCommand>
{
    public PlatformAdminLoginCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c => c.Password).NotEmpty();
    }
}
