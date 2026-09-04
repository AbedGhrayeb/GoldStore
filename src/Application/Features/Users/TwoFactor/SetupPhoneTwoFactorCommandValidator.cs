using FluentValidation;

namespace Application.Features.Users.TwoFactor;

internal sealed class SetupPhoneTwoFactorCommandValidator : AbstractValidator<SetupPhoneTwoFactorCommand>
{
    public SetupPhoneTwoFactorCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.IdToken).NotEmpty().MinimumLength(10);
    }
}
