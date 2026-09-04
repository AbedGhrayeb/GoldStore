using FluentValidation;

namespace Application.Features.Users.TwoFactor;

internal sealed class ResetPasswordWithPhoneCommandValidator : AbstractValidator<ResetPasswordWithPhoneCommand>
{
    public ResetPasswordWithPhoneCommandValidator()
    {
        RuleFor(x => x.EmailOrPhone).NotEmpty();
        RuleFor(x => x.IdToken).NotEmpty().MinimumLength(10);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}
