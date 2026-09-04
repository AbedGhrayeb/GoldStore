using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Application.Common.Models;
using Domain.Employees;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Employees.GetSalaryPayments;

internal sealed class GetSalaryPaymentsQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetSalaryPaymentsQuery, PaginatedList<SalaryPaymentResponse>>
{
    public async Task<Result<PaginatedList<SalaryPaymentResponse>>> Handle(
        GetSalaryPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<SalaryPayment> payments = context.SalaryPayments.AsNoTracking()
            .Where(p => p.TenantId == currentTenant.TenantId);

        if (!string.IsNullOrWhiteSpace(query.EmployeeName))
        {
            List<Guid> matchingEmployeeIds = await context.Employees
                .AsNoTracking()
                .Where(e => e.TenantId == currentTenant.TenantId)
                .Where(e => e.FirstName.Contains(query.EmployeeName) || e.LastName.Contains(query.EmployeeName))
                .Select(e => e.Id)
                .ToListAsync(cancellationToken);

            payments = payments.Where(p => matchingEmployeeIds.Contains(p.EmployeeId));
        }

        if (query.FromDate.HasValue)
        {
            var fromDate = DateOnly.FromDateTime(query.FromDate.Value);
            payments = payments.Where(p => p.PaymentDate >= fromDate);
        }

        if (query.ToDate.HasValue)
        {
            var toDate = DateOnly.FromDateTime(query.ToDate.Value);
            payments = payments.Where(p => p.PaymentDate <= toDate);
        }

        int totalCount = await payments.CountAsync(cancellationToken);

        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 50);

        List<Guid> employeeIds = await payments
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => p.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        List<Guid> accountIds = await payments
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => p.AccountId)
            .Distinct()
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> employeeNames = await context.Employees
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.FullName, cancellationToken);

        Dictionary<Guid, string> accountNames = await context.FinancialAccounts
            .AsNoTracking()
            .Where(a => a.TenantId == currentTenant.TenantId)
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);

        IQueryable<SalaryPaymentResponse> items = payments.Select(p => new SalaryPaymentResponse
        {
            Id = p.Id,
            EmployeeId = p.EmployeeId,
            EmployeeName = employeeNames.GetValueOrDefault(p.EmployeeId, string.Empty),
            PaymentDate = p.PaymentDate,
            ScheduledDate = p.ScheduledDate,
            SalaryAmount = p.SalaryAmount,
            DiscountAmount = p.DiscountAmount,
            Amount = p.Amount,
            AccountName = accountNames.GetValueOrDefault(p.AccountId, string.Empty),
            Notes = p.Notes,
            IsOnSchedule = p.PaymentDate == p.ScheduledDate
        });

        return await PaginatedList<SalaryPaymentResponse>.CreateAsync(items, page, pageSize);
    }
}
