using FluentValidation;

namespace Application.Employees.PaySalary;

internal sealed class PaySalaryCommandValidator : AbstractValidator<PaySalaryCommand>
{
    public PaySalaryCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("معرف الموظف مطلوب");
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("يجب اختيار حساب الدفع");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("مبلغ الدفعة يجب أن يكون قيمة موجبة");
        RuleFor(x => x.PaymentDate).NotEmpty().WithMessage("تاريخ الدفع مطلوب");
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes is not null);
    }
}
