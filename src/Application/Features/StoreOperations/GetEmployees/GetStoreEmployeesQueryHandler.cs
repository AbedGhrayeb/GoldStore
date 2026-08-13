using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.StoreOperations.GetEmployees;

internal sealed class GetStoreEmployeesQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetStoreEmployeesQuery, List<EmployeeResponse>>
{
    public async Task<Result<List<EmployeeResponse>>> Handle(
        GetStoreEmployeesQuery query,
        CancellationToken cancellationToken)
    {
        List<Guid> salesEmployeeIds = await context.SalesInvoices
            .AsNoTracking()
            .Where(s => s.TenantId == currentTenant.TenantId)
            .Where(s => s.EmployeeId.HasValue)
            .Select(s => s.EmployeeId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        List<Guid> purchaseEmployeeIds = await context.CustomerPurchaseInvoices
            .AsNoTracking()
            .Where(p => p.TenantId == currentTenant.TenantId)
            .Where(p => p.EmployeeId.HasValue)
            .Select(p => p.EmployeeId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var employeeIds = salesEmployeeIds.Concat(purchaseEmployeeIds).Distinct().ToList();
        List<EmployeeResponse> employees = await context.Employees
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new EmployeeResponse(e.Id, e.FullName))
            .ToListAsync(cancellationToken);

        return employees;
    }
}
