using FluentValidation;

namespace Application.Users.Update;

internal sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Password).MinimumLength(8).MaximumLength(100).When(c => !string.IsNullOrEmpty(c.Password));
        RuleFor(c => c.PhoneNumber).MaximumLength(30).Matches(@"^[0-9+\-\s\(\)]*$").When(c => !string.IsNullOrWhiteSpace(c.PhoneNumber)).WithMessage("رقم الهاتف غير صالح");
        RuleFor(c => c.WhatsappNumber).MaximumLength(30).Matches(@"^[0-9+\-\s\(\)]*$").When(c => !string.IsNullOrWhiteSpace(c.WhatsappNumber)).WithMessage("رقم الواتساب غير صالح");
    }
}
