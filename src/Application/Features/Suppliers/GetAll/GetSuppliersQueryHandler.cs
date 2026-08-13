using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Suppliers.GetAll;

internal sealed class GetSuppliersQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetSuppliersQuery, List<SupplierResponse>>
{
    public async Task<Result<List<SupplierResponse>>> Handle(GetSuppliersQuery query, CancellationToken cancellationToken)
    {
        List<Supplier> suppliers = await context.Suppliers
            .Include(s => s.SupplierGoldLedgerEntries)
            .Include(s => s.SupplierManufacturingLedgerEntries)
            .Include(s => s.SupplierFinancialTransactions)
            .AsNoTracking().AsNoTracking()
            .Where(s => s.TenantId == currentTenant.TenantId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var supplierIds = suppliers.Select(s => s.Id).ToList();

        var goldBalances = suppliers.SelectMany(s => s.SupplierGoldLedgerEntries)
            .Where(e => supplierIds.Contains(e.SupplierId))
            .GroupBy(e => e.SupplierId)
            .Select(g => new { SupplierId = g.Key, Balance = g.Sum(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Equivalent21KWeightInGrams : -e.Equivalent21KWeightInGrams) })
            .ToDictionary(x => x.SupplierId, x => x.Balance);

        var manufacturingBalances = suppliers.SelectMany(s => s.SupplierManufacturingLedgerEntries)
            .Where(e => supplierIds.Contains(e.SupplierId))
            .GroupBy(e => e.SupplierId)
            .Select(g => new { SupplierId = g.Key, Balance = g.Sum(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Amount : -e.Amount) })
            .ToDictionary(x => x.SupplierId, x => x.Balance);

        var lastTransactions = suppliers.SelectMany(s => s.SupplierGoldLedgerEntries)
            .Where(e => supplierIds.Contains(e.SupplierId))
            .GroupBy(e => e.SupplierId)
            .Select(g => new TransactionDate { SupplierId = g.Key, Date = g.Max(e => e.CreatedAtUtc!.Value.LocalDateTime) })
            .ToList();

        var lastMfgTransactions = suppliers.SelectMany(s => s.SupplierManufacturingLedgerEntries)
            .Where(e => supplierIds.Contains(e.SupplierId))
            .GroupBy(e => e.SupplierId)
            .Select(g => new TransactionDate { SupplierId = g.Key, Date = g.Max(e => e.CreatedAtUtc!.Value.LocalDateTime) })
            .ToList();

        var allLastDates = lastTransactions
            .Concat(lastMfgTransactions)
            .GroupBy(t => t.SupplierId)
            .ToDictionary(g => g.Key, g => g.Max(t => t.Date));

        return suppliers.Select(s => new SupplierResponse
        {
            Id = s.Id,
            Name = s.Name,
            PrimaryPhone = s.PrimaryPhone,
            SecondaryPhone = s.SecondaryPhone,
            BankAccountNumber = s.BankAccountNumber,
            CreatedAt = s.CreatedAtUtc!.Value.LocalDateTime,
            IsActive = s.IsActive,
            Notes = s.Notes,
            GoldBalance = goldBalances.TryGetValue(s.Id, out decimal goldBalance) ? goldBalance : 0,
            ManufacturingBalance = manufacturingBalances.TryGetValue(s.Id, out decimal mfgBalance) ? mfgBalance : 0,
            LastTransactionDate = allLastDates.TryGetValue(s.Id, out DateTime lastDate) ? lastDate : null
        }).ToList();
    }

    private sealed class TransactionDate
    {
        public Guid SupplierId { get; set; }
        public DateTime Date { get; set; }
    }
}
