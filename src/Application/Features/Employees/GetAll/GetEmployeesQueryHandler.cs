using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Employees;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Employees.GetAll;

internal sealed class GetEmployeesQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetEmployeesQuery, List<EmployeeResponse>>
{
    public async Task<Result<List<EmployeeResponse>>> Handle(
        GetEmployeesQuery query,
        CancellationToken cancellationToken)
    {
        List<EmployeeResponse> employees = await context.Employees
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAtUtc)
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                FullName = e.FullName,
                Role = e.Role,
                Salary = e.Salary ?? 0m,
                SalaryCycle = e.SalaryCycle,
                UserId = e.UserId,
                UserEmail = e.User != null ? e.User.Email : null,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAtUtc.HasValue ? e.CreatedAtUtc.Value.LocalDateTime : default
            })
            .ToListAsync(cancellationToken);

        foreach (EmployeeResponse employee in employees)
        {
            employee.RoleName = employee.Role.ToFriendlyString();
            employee.SalaryCycleName = employee.SalaryCycle.ToFriendlyString();
        }

        return employees;
    }
}
