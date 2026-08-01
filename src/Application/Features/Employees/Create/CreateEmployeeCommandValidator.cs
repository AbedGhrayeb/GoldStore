using FluentValidation;

namespace Application.Employees.Create;

internal sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(20);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Role).IsInEnum();
        RuleFor(c => c.Salary).GreaterThan(0);
        RuleFor(c => c.Currency).IsInEnum();
        RuleFor(c => c.SalaryCycle).IsInEnum();

        When(c => c.ConnectToUser && !c.ExistingUserId.HasValue, () =>
        {
            RuleFor(c => c.NewUserEmail).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(c => c.NewUserPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
        });
    }
}
