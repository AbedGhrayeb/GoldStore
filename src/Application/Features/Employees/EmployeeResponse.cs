using Domain.Employees;

namespace Application.Employees;

public sealed record EmployeeResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public RoleEnum Role { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public SalaryCycleEnum SalaryCycle { get; set; }
    public string SalaryCycleName { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
