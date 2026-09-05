// <copyright file="GetEmployeesQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Common;
using Domain.Employees;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Employees.GetAll;

internal sealed class GetEmployeesQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetEmployeesQuery, List<EmployeeResponse>>
{
    public async Task<Result<List<EmployeeResponse>>> Handle(
        GetEmployeesQuery query,
        CancellationToken cancellationToken)
    {
        List<EmployeeResponse> employees = await context.Employees
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .OrderByDescending(e => e.CreatedAtUtc)
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
                LastPaymentDate = e.SalaryPayments.OrderByDescending(p => p.PaymentDate).Select(p => (DateOnly?)p.PaymentDate).FirstOrDefault(),
                LastPaymentNet = e.SalaryPayments.OrderByDescending(p => p.PaymentDate).Select(p => (decimal?)p.Amount).FirstOrDefault(),
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAtUtc.HasValue ? e.CreatedAtUtc.Value.LocalDateTime : default,
            })
            .ToListAsync(cancellationToken);

        foreach (EmployeeResponse employee in employees)
        {
            employee.RoleName = employee.Role.ToFriendlyString();
            employee.SalaryCycleName = employee.SalaryCycle.ToFriendlyString();
            employee.CurrencySymbol = CurrencyExtensions.CurrencyLabels[employee.Currency].Symbol;
        }

        return employees;
    }
}
