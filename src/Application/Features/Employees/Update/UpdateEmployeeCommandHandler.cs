using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Employees;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Employees.Update;

internal sealed class UpdateEmployeeCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateEmployeeCommand, Updated>
{
    public async Task<Result<Updated>> Handle(UpdateEmployeeCommand command, CancellationToken cancellationToken)
    {
        Employee? employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (employee is null)
        {
            return EmployeeErrors.NotFound(command.Id);
        }

        Result<Updated> updateResult = employee.Update(
            command.FirstName,
            command.LastName,
            command.Role,
            command.Salary,
            command.Currency,
            command.SalaryCycle);

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        employee.IsActive = command.IsActive;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
