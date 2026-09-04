using Application.PlatformUsers.Login;
using FluentValidation;

namespace Application.Features.PlatformUsers.Login;

public class LoginPlatformUserCommandValidator : AbstractValidator<LoginPlatformUserCommand>
{
    public LoginPlatformUserCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
        RuleFor(c => c.Password).NotEmpty().MinimumLength(8).MaximumLength(20);
    }
}
