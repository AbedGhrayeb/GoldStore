using FluentValidation;

namespace Application.Features.Identity.Token.GenerateToken;

public sealed class GenerateTokenQueryValidator : AbstractValidator<GenerateTokenQuery>
{
    public GenerateTokenQueryValidator()
    {
        RuleFor(request => request.Email)
            .NotNull().NotEmpty()
            .WithErrorCode("Email_Null_Or_Empty")
            .WithMessage("البريد الإلكتروني مطلوب")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة");

        RuleFor(request => request.Password)
            .NotNull().NotEmpty()
            .WithErrorCode("Password_Null_Or_Empty")
            .WithMessage("كلمة المرور مطلوبة.")
            .MinimumLength(8).WithMessage("كلمة المرور يجب أن تكون على الأقل 8 أحرف.");
    }
}
