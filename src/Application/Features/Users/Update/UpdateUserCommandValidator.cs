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
    }
}
