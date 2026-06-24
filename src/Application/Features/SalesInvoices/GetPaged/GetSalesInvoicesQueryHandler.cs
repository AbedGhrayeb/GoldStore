using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.SalesInvoices.GetPaged;

internal sealed class GetSalesInvoicesQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetSalesInvoicesQuery, PagedSalesInvoiceResponse>
{
    private static string GetStatusLabel(SalesInvoiceStatus status) => status switch
    {
        SalesInvoiceStatus.Draft => "مسودة",
        SalesInvoiceStatus.Completed => "مكتملة",
        SalesInvoiceStatus.PartiallyPaid => "مدفوعة جزئياً",
        SalesInvoiceStatus.Cancelled => "ملغاة",
        _ => status.ToString()
    };

    private static string GetCurrencySymbol(string currency) => currency switch
    {
        "Jod" => "د.أ",
        "Usd" => "$",
        "Ils" => "₪",
        _ => currency
    };

    public async Task<Result<PagedSalesInvoiceResponse>> Handle(GetSalesInvoicesQuery query, CancellationToken cancellationToken)
    {
        IQueryable<SalesInvoice> invoicesQuery = context.SalesInvoices.AsNoTracking();

        if (query.FromDate.HasValue)
        {
            invoicesQuery = invoicesQuery.Where(i => i.Date >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            invoicesQuery = invoicesQuery.Where(i => i.Date <= query.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<SalesInvoiceStatus>(query.Status, out var status))
        {
            invoicesQuery = invoicesQuery.Where(i => i.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search.Trim();
            invoicesQuery = invoicesQuery.Where(i =>
                i.InvoiceNumber.Contains(search) ||
                i.CustomerName.Contains(search));
        }

        int totalCount = await invoicesQuery.CountAsync(cancellationToken);

        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        List<Guid> userIds = await invoicesQuery
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => i.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> userNames = await context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}", cancellationToken);

        List<SalesInvoice> pagedData = await invoicesQuery
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        List<Guid> invoiceIds = pagedData.Select(i => i.Id).ToList();

        List<SalesInvoiceItem> allItems = await context.SalesInvoiceItems
            .AsNoTracking()
            .Where(i => invoiceIds.Contains(i.SalesInvoiceId))
            .ToListAsync(cancellationToken);

        var itemsByInvoice = allItems.GroupBy(i => i.SalesInvoiceId)
            .ToDictionary(g => g.Key, g => g.ToList());

        List<SalesInvoiceResponse> items = pagedData.Select(invoice =>
        {
            List<SalesInvoiceItem> invoiceItems = itemsByInvoice.GetValueOrDefault(invoice.Id, []);

            return new SalesInvoiceResponse
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                CustomerName = invoice.CustomerName,
                CustomerPhone = invoice.CustomerPhone,
                Date = invoice.Date,
                Currency = invoice.Currency.ToString(),
                TotalAmount = invoice.TotalAmount,
                AmountPaid = invoice.AmountPaid,
                RemainingBalance = invoice.RemainingBalance,
                PaymentMethod = invoice.PaymentMethod?.ToString(),
                Status = invoice.Status.ToString(),
                StatusLabel = GetStatusLabel(invoice.Status),
                UserName = userNames.GetValueOrDefault(invoice.UserId, "—"),
                Notes = invoice.Notes,
                CreatedAt = invoice.CreatedAt,
                Items = invoiceItems.Select(ii => new SalesInvoiceItemResponse
                {
                    Id = ii.Id,
                    Karat = (int)ii.Karat,
                    WeightInGrams = ii.WeightInGrams,
                    Equivalent21KWeightInGrams = ii.Equivalent21KWeightInGrams,
                    PricePerGram = ii.PricePerGram,
                    GoldAmount = ii.GoldAmount
                }).ToList()
            };
        }).ToList();

        return new PagedSalesInvoiceResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
