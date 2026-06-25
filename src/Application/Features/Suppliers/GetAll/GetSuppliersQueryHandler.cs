using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Suppliers.GetAll;

internal sealed class GetSuppliersQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetSuppliersQuery, List<SupplierResponse>>
{
    public async Task<Result<List<SupplierResponse>>> Handle(GetSuppliersQuery query, CancellationToken cancellationToken)
    {
        List<SupplierResponse> suppliers = await context.Suppliers.AsNoTracking()
            .OrderByDescending(s => s)
            .Select(s => new SupplierResponse
            {
                Id = s.Id,
                Name = s.Name,
                PrimaryPhone = s.PrimaryPhone,
                SecondaryPhone = s.SecondaryPhone,
                BankAccountNumber = s.BankAccountNumber,
                Notes = s.Notes,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var supplierIds = suppliers.Select(s => s.Id).ToList();

        Dictionary<Guid, decimal> goldBalances = await context.SupplierGoldLedgerEntries
            .Where(e => supplierIds.Contains(e.SupplierId))
            .GroupBy(e => e.SupplierId)
            .Select(g => new { SupplierId = g.Key, Balance = g.Sum(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Equivalent21KWeightInGrams : -e.Equivalent21KWeightInGrams) })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Balance, cancellationToken);

        Dictionary<Guid, decimal> manufacturingBalances = await context.SupplierManufacturingLedgerEntries
            .Where(e => supplierIds.Contains(e.SupplierId))
            .GroupBy(e => e.SupplierId)
            .Select(g => new { SupplierId = g.Key, Balance = g.Sum(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Amount : -e.Amount) })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Balance, cancellationToken);

        List<TransactionDate> lastTransactions = await context.SupplierGoldLedgerEntries
            .Where(e => supplierIds.Contains(e.SupplierId))
            .GroupBy(e => e.SupplierId)
            .Select(g => new TransactionDate { SupplierId = g.Key, Date = g.Max(e => e.Date) })
            .ToListAsync(cancellationToken);

        List<TransactionDate> lastMfgTransactions = await context.SupplierManufacturingLedgerEntries
            .Where(e => supplierIds.Contains(e.SupplierId))
            .GroupBy(e => e.SupplierId)
            .Select(g => new TransactionDate { SupplierId = g.Key, Date = g.Max(e => e.Date) })
            .ToListAsync(cancellationToken);

        var allLastDates = lastTransactions
            .Concat(lastMfgTransactions)
            .GroupBy(t => t.SupplierId)
            .ToDictionary(g => g.Key, g => g.Max(t => t.Date));

        suppliers = suppliers.Select(s =>
        {
            goldBalances.TryGetValue(s.Id, out decimal goldBalance);
            manufacturingBalances.TryGetValue(s.Id, out decimal mfgBalance);
            allLastDates.TryGetValue(s.Id, out DateTime lastDate);

            return s with
            {
                GoldBalance = goldBalance,
                ManufacturingBalance = mfgBalance,
                LastTransactionDate = allLastDates.ContainsKey(s.Id) ? lastDate : null
            };
        }).ToList();

        return suppliers;
    }

    private sealed class TransactionDate
    {
        public Guid SupplierId { get; set; }
        public DateTime Date { get; set; }
    }
}
