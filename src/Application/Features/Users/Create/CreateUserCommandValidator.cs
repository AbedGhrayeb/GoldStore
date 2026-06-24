using FluentValidation;

namespace Application.Users.Create;

internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c => c.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}
