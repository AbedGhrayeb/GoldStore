// <copyright file="GetSalesInvoiceByIdQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.SalesInvoices.GetById;

internal sealed class GetSalesInvoiceByIdQueryHandler(
    IApplicationDbContext context,
    ICurrentTenant currentTenant)
    : IQueryHandler<GetSalesInvoiceByIdQuery, SalesInvoiceResponse>
{
    public async Task<Result<SalesInvoiceResponse>> Handle(
        GetSalesInvoiceByIdQuery query,
        CancellationToken cancellationToken)
    {
        SalesInvoice? invoice = await context.SalesInvoices
            .Include(item => item.SaleInvoiceItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == query.Id && item.TenantId == currentTenant.TenantId,
                cancellationToken);

        if (invoice is null)
        {
            return SalesInvoiceErrors.NotFound(query.Id);
        }

        string employeeName = "—";
        if (invoice.EmployeeId is { } employeeId)
        {
            employeeName = await context.Employees
                .AsNoTracking()
                .Where(employee => employee.Id == employeeId && employee.TenantId == currentTenant.TenantId)
                .Select(employee => employee.FirstName + " " + employee.LastName)
                .FirstOrDefaultAsync(cancellationToken) ?? "—";
        }

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
            StatusLabel = invoice.Status.ToStatusLabel(),
            UserName = employeeName,
            Notes = invoice.Notes,
            CreatedAt = invoice.CreatedAtUtc?.LocalDateTime ?? default,
            Items = invoice.SaleInvoiceItems.Select(item => new SalesInvoiceItemResponse
            {
                Id = item.Id,
                Karat = (int)item.Karat,
                WeightInGrams = item.WeightInGrams,
                Equivalent21KWeightInGrams = item.Equivalent21KWeightInGrams,
                PricePerGram = item.PricePerGram,
                GoldAmount = item.GoldAmount,
            }).ToList(),
        };
    }
}
