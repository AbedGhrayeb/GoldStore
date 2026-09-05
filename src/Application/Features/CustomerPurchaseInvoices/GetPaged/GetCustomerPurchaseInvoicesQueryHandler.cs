// <copyright file="GetCustomerPurchaseInvoicesQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Application.Common.Models;
using Domain.CustomerPurchases;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.CustomerPurchaseInvoices.GetPaged;

internal sealed class GetCustomerPurchaseInvoicesQueryHandler(
    IApplicationDbContext context,
    ICurrentTenant currentTenant)
    : IQueryHandler<GetCustomerPurchaseInvoicesQuery, PaginatedList<CustomerPurchaseInvoiceResponse>>
{
    public async Task<Result<PaginatedList<CustomerPurchaseInvoiceResponse>>> Handle(
        GetCustomerPurchaseInvoicesQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<CustomerPurchaseInvoice> invoicesQuery = context.CustomerPurchaseInvoices
            .Include(x => x.Items)
            .AsNoTracking()
            .Where(i => i.TenantId == currentTenant.TenantId);

        if (query.FromDate.HasValue)
        {
            invoicesQuery = invoicesQuery.Where(i => i.Date >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            invoicesQuery = invoicesQuery.Where(i => i.Date <= query.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search.Trim();
            invoicesQuery = invoicesQuery.Where(i =>
                i.InvoiceNumber.Contains(search) ||
                i.SellerName.Contains(search));
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

        List<CustomerPurchaseInvoice> pageItems = await invoicesQuery
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = pageItems.Select(invoice => new CustomerPurchaseInvoiceResponse
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            SellerName = invoice.SellerName,
            SellerPhone = invoice.SellerPhone,
            SellerIdNumber = invoice.SellerIdNumber,
            SellerYearOfBirth = invoice.SellerYearOfBirth,
            SellerAddress = invoice.SellerAddress,
            EmployeeName = invoice.EmployeeId.HasValue
                ? employeeNames.GetValueOrDefault(invoice.EmployeeId.Value, "—")
                : "—",
            Date = invoice.Date,
            Currency = invoice.Currency.ToString(),
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid,
            RemainingBalance = invoice.RemainingBalance,
            PaymentMethod = invoice.PaymentMethod.ToString(),
            SellerAccountNumber = invoice.SellerAccountNumber,
            Notes = invoice.Notes,
            CreatedAt = invoice.CreatedAtUtc!.Value.LocalDateTime,
            Items = invoice.Items.Select(item => new CustomerPurchaseInvoiceItemResponse
            {
                Id = item.Id,
                CategoryId = item.CategoryId,
                Karat = (int)item.Karat,
                WeightInGrams = item.WeightInGrams,
                Equivalent21KWeightInGrams = item.Equivalent21KWeightInGrams,
                PricePerGram = item.PricePerGram,
                GoldAmount = item.GoldAmount,
            }).ToList(),
        }).ToList();

        return new PaginatedList<CustomerPurchaseInvoiceResponse>(items, page, pageSize, totalCount);
    }
}
