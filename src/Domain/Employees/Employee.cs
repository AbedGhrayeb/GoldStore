using Domain.Common;
using Domain.Users;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Employees;

public sealed class Employee : AuditableEntity
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string FullName => $"{FirstName} {LastName}";
    public RoleEnum Role { get; private set; }
    public decimal? Salary { get; private set; }
    public Currency Currency { get; private set; }
    public SalaryCycleEnum SalaryCycle { get; private set; }
    public Guid? UserId { get; private set; }
    public User User { get; set; }
    public ICollection<SalaryPayment> SalaryPayments { get; set; } = [];
    private Employee()
    {

    }
    private Employee(Guid id, string firstName, string lastName, RoleEnum role, decimal salary, Currency currency, SalaryCycleEnum salaryCycle, Guid? userId) : base(id)
    {

        FirstName = firstName;
        LastName = lastName;
        Role = role;
        SalaryCycle = salaryCycle;
        Salary = salary;
        Currency = currency;
        UserId = userId;

    }

    public static Result<Employee> Create(string firstName, string lastName, RoleEnum role, decimal salary, Currency currency, SalaryCycleEnum salaryCycle, Guid? userId)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return EmployeeErrors.FirstNameRequired;
        }
        if (string.IsNullOrWhiteSpace(lastName))
        {
            return EmployeeErrors.LastNameRequired;
        }
        if (!Enum.IsDefined(typeof(RoleEnum), role))
        {
            return EmployeeErrors.RoleRequired;
        }
        if (salary <= 0)
        {
            return EmployeeErrors.SalaryMustbePositive;
        }
        if (!Enum.IsDefined(typeof(Currency), currency))
        {
            return EmployeeErrors.CurrencyRequired;
        }
        if (!Enum.IsDefined(typeof(SalaryCycleEnum), salaryCycle))
        {
            return EmployeeErrors.SalaryCycleRequired;
        }
        return new Employee(Guid.CreateVersion7(), firstName, lastName, role, salary, currency, salaryCycle, userId);
    }

    public Result<Updated> Update(string firstName, string lastName, RoleEnum role, decimal salary, Currency currency, SalaryCycleEnum salaryCycle)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return EmployeeErrors.FirstNameRequired;
        }
        if (string.IsNullOrWhiteSpace(lastName))
        {
            return EmployeeErrors.LastNameRequired;
        }
        if (!Enum.IsDefined(typeof(RoleEnum), role))
        {
            return EmployeeErrors.RoleRequired;
        }
        if (salary <= 0)
        {
            return EmployeeErrors.SalaryMustbePositive;
        }
        if (!Enum.IsDefined(typeof(Currency), currency))
        {
            return EmployeeErrors.CurrencyRequired;
        }
        if (!Enum.IsDefined(typeof(SalaryCycleEnum), salaryCycle))
        {
            return EmployeeErrors.SalaryCycleRequired;
        }

        FirstName = firstName;
        LastName = lastName;
        Role = role;
        Salary = salary;
        Currency = currency;
        SalaryCycle = salaryCycle;

        return Result.Updated;
    }
}
