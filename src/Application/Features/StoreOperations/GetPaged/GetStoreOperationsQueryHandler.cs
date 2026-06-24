using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.StoreOperations.Shared;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.StoreOperations.GetPaged;

internal sealed class GetStoreOperationsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetStoreOperationsQuery, PagedStoreOperationsResponse>
{
    private static string GetStatusLabel(SalesInvoiceStatus? status) => status switch
    {
        SalesInvoiceStatus.Draft => "مسودة",
        SalesInvoiceStatus.Completed => "مكتملة",
        SalesInvoiceStatus.PartiallyPaid => "مدفوعة جزئياً",
        SalesInvoiceStatus.Cancelled => "ملغاة",
        _ => null
    };

    private static string GetPaymentMethodLabel(PaymentMethod? method) => method switch
    {
        PaymentMethod.Cash => "نقدي",
        PaymentMethod.Bank => "مصرفي",
        _ => null
    };

    private static string GetCurrencySymbol(string currency) => currency switch
    {
        "Jod" => "د.أ",
        "Usd" => "$",
        "Ils" => "₪",
        _ => currency
    };

    public async Task<Result<PagedStoreOperationsResponse>> Handle(
        GetStoreOperationsQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<StoreOperationRow> salesQuery = context.SalesInvoices
            .AsNoTracking()
            .Select(s => new StoreOperationRow
            {
                Id = s.Id,
                InvoiceNumber = s.InvoiceNumber,
                OperationType = "Sale",
                Date = s.Date,
                CounterpartyName = s.CustomerName,
                CounterpartyPhone = s.CustomerPhone,
                EmployeeName = s.SellerName,
                Currency = s.Currency.ToString(),
                TotalAmount = s.TotalAmount,
                AmountPaid = s.AmountPaid,
                RemainingBalance = s.RemainingBalance,
                PaymentMethodInt = s.PaymentMethod,
                AccountId = s.AccountId,
                StatusInt = s.Status,
                Notes = s.Notes
            });

        IQueryable<StoreOperationRow> purchasesQuery = context.CustomerPurchaseInvoices
            .AsNoTracking()
            .Select(p => new StoreOperationRow
            {
                Id = p.Id,
                InvoiceNumber = p.InvoiceNumber,
                OperationType = "Buy",
                Date = p.Date,
                CounterpartyName = p.SellerName,
                CounterpartyPhone = p.SellerPhone,
                EmployeeName = p.BuyerName,
                Currency = p.Currency.ToString(),
                TotalAmount = p.TotalAmount,
                AmountPaid = p.AmountPaid,
                RemainingBalance = p.TotalAmount - p.AmountPaid,
                PaymentMethodInt = p.PaymentMethod,
                AccountId = p.AccountId,
                StatusInt = null,
                Notes = p.Notes
            });

        IQueryable<StoreOperationRow> combined = salesQuery.Concat(purchasesQuery);

        if (query.FromDate.HasValue)
        {
            DateTime from = query.FromDate.Value;
            combined = combined.Where(r => r.Date >= from);
        }

        if (query.ToDate.HasValue)
        {
            DateTime to = query.ToDate.Value;
            combined = combined.Where(r => r.Date <= to);
        }

        if (!string.IsNullOrWhiteSpace(query.OperationType)
            && query.OperationType is "Sale" or "Buy")
        {
            string type = query.OperationType;
            combined = combined.Where(r => r.OperationType == type);
        }

        if (!string.IsNullOrWhiteSpace(query.EmployeeName))
        {
            string employee = query.EmployeeName.Trim();
            combined = combined.Where(r => r.EmployeeName == employee);
        }

        if (query.AccountId.HasValue)
        {
            Guid accountId = query.AccountId.Value;
            combined = combined.Where(r => r.AccountId == accountId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search.Trim();
            combined = combined.Where(r =>
                r.InvoiceNumber.Contains(search) ||
                r.CounterpartyName.Contains(search));
        }

        int totalCount = await combined.CountAsync(cancellationToken);

        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        List<StoreOperationRow> rows = await combined
            .OrderByDescending(r => r.Date)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var saleIds = rows.Where(r => r.OperationType == "Sale").Select(r => r.Id).ToList();
        var purchaseIds = rows.Where(r => r.OperationType == "Buy").Select(r => r.Id).ToList();
        var accountIds = rows.Select(r => r.AccountId).Where(a => a.HasValue).Distinct().ToList();

        Dictionary<Guid, int> saleItemCounts = await context.SalesInvoiceItems
            .AsNoTracking()
            .Where(i => saleIds.Contains(i.SalesInvoiceId))
            .GroupBy(i => i.SalesInvoiceId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        Dictionary<Guid, int> purchaseItemCounts = await context.CustomerPurchaseInvoiceItems
            .AsNoTracking()
            .Where(i => purchaseIds.Contains(i.CustomerPurchaseInvoiceId))
            .GroupBy(i => i.CustomerPurchaseInvoiceId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        Dictionary<Guid, string> accountNames = await context.FinancialAccounts
            .AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);

        var items = rows.Select(row =>
        {
            int itemCount = row.OperationType == "Sale"
                ? saleItemCounts.GetValueOrDefault(row.Id, 0)
                : purchaseItemCounts.GetValueOrDefault(row.Id, 0);

            string? paymentMethod = row.PaymentMethodInt.HasValue
                ? ((PaymentMethod)row.PaymentMethodInt.Value).ToString()
                : null;

            string? status = row.StatusInt.HasValue
                ? ((SalesInvoiceStatus)row.StatusInt.Value).ToString()
                : null;

            return new StoreOperationResponse
            {
                Id = row.Id,
                InvoiceNumber = row.InvoiceNumber,
                OperationType = row.OperationType,
                OperationTypeLabel = row.OperationType == "Sale" ? "بيع" : "شراء",
                Date = row.Date,
                CounterpartyName = row.CounterpartyName,
                CounterpartyPhone = row.CounterpartyPhone,
                EmployeeName = row.EmployeeName,
                Currency = row.Currency,
                CurrencySymbol = GetCurrencySymbol(row.Currency),
                TotalAmount = row.TotalAmount,
                AmountPaid = row.AmountPaid,
                RemainingBalance = row.RemainingBalance,
                PaymentMethod = paymentMethod,
                PaymentMethodLabel = GetPaymentMethodLabel(row.PaymentMethodInt),
                AccountId = row.AccountId,
                AccountName = row.AccountId.HasValue
                    ? accountNames.GetValueOrDefault(row.AccountId.Value)
                    : null,
                Status = status,
                StatusLabel = GetStatusLabel(row.StatusInt),
                ItemsCount = itemCount,
                Notes = row.Notes
            };
        }).ToList();

        return new PagedStoreOperationsResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private sealed record StoreOperationRow
    {
        public Guid Id { get; init; }
        public string InvoiceNumber { get; init; } = string.Empty;
        public string OperationType { get; init; } = string.Empty;
        public DateTime Date { get; init; }
        public string CounterpartyName { get; init; } = string.Empty;
        public string? CounterpartyPhone { get; init; }
        public string? EmployeeName { get; init; }
        public string Currency { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
        public decimal AmountPaid { get; init; }
        public decimal RemainingBalance { get; init; }
        public PaymentMethod? PaymentMethodInt { get; init; }
        public Guid? AccountId { get; init; }
        public SalesInvoiceStatus? StatusInt { get; init; }
        public string? Notes { get; init; }
    }
}
