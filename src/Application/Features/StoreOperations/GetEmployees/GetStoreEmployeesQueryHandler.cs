using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.StoreOperations.GetEmployees;

internal sealed class GetStoreEmployeesQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetStoreEmployeesQuery, List<string>>
{
    public async Task<Result<List<string>>> Handle(
        GetStoreEmployeesQuery query,
        CancellationToken cancellationToken)
    {
        List<string> salesEmployees = await context.SalesInvoices
            .AsNoTracking()
            .Where(s => s.SellerName != null && s.SellerName != string.Empty)
            .Select(s => s.SellerName!)
            .Distinct()
            .ToListAsync(cancellationToken);

        List<string> purchaseEmployees = await context.CustomerPurchaseInvoices
            .AsNoTracking()
            .Select(p => p.BuyerName)
            .Distinct()
            .ToListAsync(cancellationToken);

        var employees = salesEmployees
            .Concat(purchaseEmployees)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return employees;
    }
}
