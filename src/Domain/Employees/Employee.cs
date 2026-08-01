using SharedKernel;
using SharedKernel.Result;

namespace Domain.Employees;

public sealed class Employee : AuditableEntity
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string FullName => $"{FirstName} {LastName}";
    public string Title { get; private set; }
    public decimal? Salary { get; private set; }
    public Guid? UserId { get; private set; }
    private Employee()
    {

    }
    private Employee(Guid id, string firstName, string lastName, string title, decimal salary, Guid? userId) : base(id)
    {
        {
            FirstName = firstName;
            LastName = lastName;
            Title = title;
            Salary = salary;
            UserId = userId;
        }
    }

    public static Result<Employee> Create(string firstName, string lastName, string title, decimal salary, Guid? userId)
    {
        return new Employee(Guid.CreateVersion7(), firstName, lastName, title, salary, userId);
    }
}
