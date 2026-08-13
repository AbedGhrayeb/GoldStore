using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Application.Common.Models;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.SalesInvoices.GetPaged;

internal sealed class GetSalesInvoicesQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetSalesInvoicesQuery, PaginatedList<SalesInvoiceResponse>>
{
    public async Task<Result<PaginatedList<SalesInvoiceResponse>>> Handle(GetSalesInvoicesQuery query, CancellationToken cancellationToken)
    {
        IQueryable<SalesInvoice> invoicesQuery = context.SalesInvoices.
            Include(x => x.SaleInvoiceItems).AsNoTracking().Where(i => i.TenantId == currentTenant.TenantId);

        if (query.FromDate.HasValue)
        {
            invoicesQuery = invoicesQuery.Where(i => i.Date >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            invoicesQuery = invoicesQuery.Where(i => i.Date <= query.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<SalesInvoiceStatus>(query.Status, out SalesInvoiceStatus status))
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

        List<Guid> employeeIds = await invoicesQuery
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.Id)
            .Where(i => i.EmployeeId.HasValue)
            .Select(i => i.EmployeeId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> employeeNames = await context.Employees
            .AsNoTracking()
            .Where(u => u.TenantId == currentTenant.TenantId && employeeIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}", cancellationToken);

        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 50);

        int totalCount = await invoicesQuery.CountAsync(cancellationToken);

        List<SalesInvoice> pageItems = await invoicesQuery
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        List<SalesInvoiceResponse> items = pageItems.Select(invoice => new SalesInvoiceResponse
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
            PaymentMethod = invoice.PaymentMethod.ToString(),
            Status = invoice.Status.ToString(),
            StatusLabel = invoice.Status.ToStatusLabel(),
            UserName = invoice.EmployeeId.HasValue
                ? employeeNames.GetValueOrDefault(invoice.EmployeeId.Value, "—")
                : "—",
            Notes = invoice.Notes,
            CreatedAt = invoice.CreatedAtUtc!.Value.LocalDateTime,
            Items = invoice.SaleInvoiceItems.Select(ii => new SalesInvoiceItemResponse
            {
                Id = ii.Id,
                Karat = (int)ii.Karat,
                WeightInGrams = ii.WeightInGrams,
                Equivalent21KWeightInGrams = ii.Equivalent21KWeightInGrams,
                PricePerGram = ii.PricePerGram,
                GoldAmount = ii.GoldAmount
            }).ToList()
        }).ToList();

        return new PaginatedList<SalesInvoiceResponse>(items, page, pageSize, totalCount);
    }
}
