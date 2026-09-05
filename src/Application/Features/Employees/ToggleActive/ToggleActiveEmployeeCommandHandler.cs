// <copyright file="ToggleActiveEmployeeCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Employees;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Employees.ToggleActive;

internal sealed class ToggleActiveEmployeeCommandHandler(IApplicationDbContext context)
    : ICommandHandler<ToggleActiveEmployeeCommand, Updated>
{
    public async Task<Result<Updated>> Handle(ToggleActiveEmployeeCommand command, CancellationToken cancellationToken)
    {
        Employee? employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (employee is null)
        {
            return EmployeeErrors.NotFound(command.Id);
        }

        employee.IsActive = !employee.IsActive;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
