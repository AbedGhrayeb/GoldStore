using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.SupplierFinancialTransactions.GetPaged;

internal sealed class GetPagedSupplierFinancialTransactionsQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetPagedSupplierFinancialTransactionsQuery, PagedSupplierFinancialTransactionResponse>
{
    public async Task<Result<PagedSupplierFinancialTransactionResponse>> Handle(
        GetPagedSupplierFinancialTransactionsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<SupplierFinancialTransaction> transactionsQuery = context.SupplierFinancialTransactions
            .AsNoTracking();

        if (query.SupplierId.HasValue)
        { transactionsQuery = transactionsQuery.Where(t => t.SupplierId == query.SupplierId.Value); }

        if (query.Direction.HasValue)
        { transactionsQuery = transactionsQuery.Where(t => (int)t.Direction == query.Direction.Value); }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search.Trim();
            transactionsQuery = transactionsQuery.Where(t =>
                context.Suppliers.Any(s => s.Id == t.SupplierId && s.Name.Contains(search)));
        }

        int totalCount = await transactionsQuery.CountAsync(cancellationToken);

        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        List<SupplierFinancialTransaction> pagedData = await transactionsQuery
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var transactionIds = pagedData.Select(t => t.Id).ToList();

        Dictionary<Guid, string> supplierNames = await context.Suppliers
            .Where(s => pagedData.Select(t => t.SupplierId).Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        List<SupplierFinancialLedgerEntry> allEntries = await context.SupplierFinancialLedgerEntries
            .AsNoTracking()
            .Where(e => transactionIds.Contains(e.SupplierFinancialTransactionId))
            .ToListAsync(cancellationToken);

        var balances = allEntries
            .GroupBy(e => e.SupplierFinancialTransactionId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Amount : -e.Amount));

        var items = pagedData.Select(t =>
        {
            decimal balance = balances.GetValueOrDefault(t.Id, 0m);
            return new SupplierFinancialTransactionResponse
            {
                Id = t.Id,
                SupplierId = t.SupplierId,
                SupplierName = supplierNames.GetValueOrDefault(t.SupplierId, "غير معروف"),
                Direction = t.Direction.ToString(),
                DirectionLabel = GetDirectionLabel(t.Direction),
                Amount = t.Amount,
                Currency = t.Currency.ToString(),
                AccountId = t.AccountId,
                Notes = t.Notes,
                CreatedAt = t.CreatedAtUtc!.Value.LocalDateTime,
                OutstandingBalance = balance,
                OutstandingBalanceDisplay = balance.ToString("N3")
            };
        }).ToList();

        return new PagedSupplierFinancialTransactionResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static string GetDirectionLabel(SupplierFinancialTransactionDirection direction) => direction switch
    {
        SupplierFinancialTransactionDirection.FromSupplier => "له",
        SupplierFinancialTransactionDirection.ToSupplier => "لنا",
        _ => direction.ToString()
    };
}
