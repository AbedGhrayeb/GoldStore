// <copyright file="GetCustomerPurchaseInvoiceByIdQueryHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.CustomerPurchases;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.CustomerPurchaseInvoices.GetById;

internal sealed class GetCustomerPurchaseInvoiceByIdQueryHandler(
    IApplicationDbContext context,
    ICurrentTenant currentTenant)
    : IQueryHandler<GetCustomerPurchaseInvoiceByIdQuery, CustomerPurchaseInvoiceResponse>
{
    public async Task<Result<CustomerPurchaseInvoiceResponse>> Handle(
        GetCustomerPurchaseInvoiceByIdQuery query,
        CancellationToken cancellationToken)
    {
        CustomerPurchaseInvoice? invoice = await context.CustomerPurchaseInvoices
            .Include(item => item.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == query.Id && item.TenantId == currentTenant.TenantId,
                cancellationToken);

        if (invoice is null)
        {
            return CustomerPurchaseInvoiceErrors.NotFound(query.Id);
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

        return new CustomerPurchaseInvoiceResponse
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            SellerName = invoice.SellerName,
            SellerPhone = invoice.SellerPhone,
            SellerIdNumber = invoice.SellerIdNumber,
            SellerYearOfBirth = invoice.SellerYearOfBirth,
            SellerAddress = invoice.SellerAddress,
            EmployeeName = employeeName,
            Date = invoice.Date,
            Currency = invoice.Currency.ToString(),
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid,
            RemainingBalance = invoice.RemainingBalance,
            PaymentMethod = invoice.PaymentMethod.ToString(),
            SellerAccountNumber = invoice.SellerAccountNumber,
            Notes = invoice.Notes,
            CreatedAt = invoice.CreatedAtUtc?.LocalDateTime ?? default,
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
        };
    }
}
