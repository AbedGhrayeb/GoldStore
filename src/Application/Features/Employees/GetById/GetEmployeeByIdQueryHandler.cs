// <copyright file="GetEmployeeByIdQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Common;
using Domain.Employees;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Employees.GetById;

internal sealed class GetEmployeeByIdQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetEmployeeByIdQuery, EmployeeResponse>
{
    public async Task<Result<EmployeeResponse>> Handle(
        GetEmployeeByIdQuery query,
        CancellationToken cancellationToken)
    {
        EmployeeResponse? employee = await context.Employees
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .Where(e => e.Id == query.Id)
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                FullName = e.FullName,
                Role = e.Role,
                Salary = e.Salary ?? 0m,
                Currency = e.Currency,
                SalaryCycle = e.SalaryCycle,
                UserId = e.UserId,
                UserEmail = e.User != null ? e.User.Email : null,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAtUtc.HasValue ? e.CreatedAtUtc.Value.LocalDateTime : default,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null)
        {
            return EmployeeErrors.NotFound(query.Id);
        }

        employee.RoleName = employee.Role.ToFriendlyString();
        employee.SalaryCycleName = employee.SalaryCycle.ToFriendlyString();
        employee.CurrencySymbol = CurrencyExtensions.CurrencyLabels[employee.Currency].Symbol;

        return employee;
    }
}
