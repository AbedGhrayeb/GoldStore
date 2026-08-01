using FluentValidation;

namespace Application.Employees.Update;

internal sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(20);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Role).IsInEnum();
        RuleFor(c => c.Salary).GreaterThan(0);
        RuleFor(c => c.SalaryCycle).IsInEnum();
    }
}
