using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Suppliers.GetById;

internal sealed class GetSupplierByIdQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetSupplierByIdQuery, SupplierDetailResponse>
{
    public async Task<Result<SupplierDetailResponse>> Handle(GetSupplierByIdQuery query, CancellationToken cancellationToken)
    {
        var supplier = await context.Suppliers
            .Where(s => s.Id == query.Id)
            .Select(s => new { s.Id, s.Name, s.PrimaryPhone, s.SecondaryPhone, s.BankAccountNumber, s.Notes, s.IsActive, s.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (supplier is null)
        {
            return Result.Failure<SupplierDetailResponse>(SupplierErrors.NotFound(query.Id));
        }

        decimal goldBalance = await context.SupplierGoldLedgerEntries
            .Where(e => e.SupplierId == query.Id)
            .SumAsync(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Equivalent21KWeightInGrams : -e.Equivalent21KWeightInGrams, cancellationToken);

        decimal manufacturingBalance = await context.SupplierManufacturingLedgerEntries
            .Where(e => e.SupplierId == query.Id)
            .SumAsync(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Amount : -e.Amount, cancellationToken);

        List<SupplierTransactionResponse> recentTransactions = await GetRecentTransactions(query.Id, cancellationToken);

        return new SupplierDetailResponse
        {
            Id = supplier.Id,
            Name = supplier.Name,
            PrimaryPhone = supplier.PrimaryPhone,
            SecondaryPhone = supplier.SecondaryPhone,
            BankAccountNumber = supplier.BankAccountNumber,
            Notes = supplier.Notes,
            IsActive = supplier.IsActive,
            CreatedAt = supplier.CreatedAt,
            GoldBalance = goldBalance,
            ManufacturingBalance = manufacturingBalance,
            RecentTransactions = recentTransactions
        };
    }

    private async Task<List<SupplierTransactionResponse>> GetRecentTransactions(Guid supplierId, CancellationToken cancellationToken)
    {
        List<SupplierTransactionResponse> goldEntries = await context.SupplierGoldLedgerEntries
            .Where(e => e.SupplierId == supplierId)
            .OrderByDescending(e => e.Date)
            .Take(5)
            .Select(e => new SupplierTransactionResponse
            {
                Id = e.Id,
                Description = e.ReferenceType == SupplierGoldReferenceType.SupplierDelivery ? "توريد ذهب" :
                              e.ReferenceType == SupplierGoldReferenceType.SupplierScrapPayment ? "استلام كسر ذهب" :
                              "تسوية يدوية",
                Date = e.Date,
                Type = "ذهب",
                Amount = e.Equivalent21KWeightInGrams,
                Unit = "جم",
                Direction = e.MovementType == SupplierBalanceMovementType.Increase ? "+" : "-"
            })
            .ToListAsync(cancellationToken);

        List<SupplierTransactionResponse> mfgEntries = await context.SupplierManufacturingLedgerEntries
            .Where(e => e.SupplierId == supplierId)
            .OrderByDescending(e => e.Date)
            .Take(5)
            .Select(e => new SupplierTransactionResponse
            {
                Id = e.Id,
                Description = e.ReferenceType == SupplierManufacturingReferenceType.SupplierDelivery ? "أجور تصنيع" :
                              e.ReferenceType == SupplierManufacturingReferenceType.SupplierManufacturingPayment ? "دفعة نقدية - أجور" :
                              "تسوية يدوية",
                Date = e.Date,
                Type = "تصنيع",
                Amount = e.Amount,
                Unit = "د.إ",
                Direction = e.MovementType == SupplierBalanceMovementType.Increase ? "+" : "-"
            })
            .ToListAsync(cancellationToken);

        return goldEntries.Concat(mfgEntries)
            .OrderByDescending(t => t.Date)
            .Take(10)
            .ToList();
    }
}
