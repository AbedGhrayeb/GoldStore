using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Application.Features.StoreOperations.Shared;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.StoreOperations.GetDetail;

internal sealed class GetStoreOperationDetailQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetStoreOperationDetailQuery, StoreOperationDetailResponse>
{

    private static string GetCurrencySymbol(string currency) => currency switch
    {
        "JOD" => "د.أ",
        "USD" => "$",
        "ILS" => "₪",
        _ => currency
    };

    public async Task<Result<StoreOperationDetailResponse>> Handle(
        GetStoreOperationDetailQuery query,
        CancellationToken cancellationToken)
    {
        if (query.OperationType is "Sale")
        {
            return await GetSaleDetail(query.Id, cancellationToken);
        }

        if (query.OperationType is "Buy")
        {
            return await GetPurchaseDetail(query.Id, cancellationToken);
        }

        return
            Error.Failure("StoreOperations.InvalidType", "نوع العملية غير صالح");
    }

    private async Task<Result<StoreOperationDetailResponse>> GetSaleDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        Domain.Sales.SalesInvoice? invoice = await context.SalesInvoices.Include(i => i.SaleInvoiceItems)
            .ThenInclude(ii => ii.Category).Include(i => i.FinancialAccount)
            .AsNoTracking()
            .Where(s => s.TenantId == currentTenant.TenantId)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (invoice is null)
        {
            return SalesInvoiceErrors.NotFound(id);
        }

        var items = invoice.SaleInvoiceItems.ToList();

        string? accountName = null;
        if (invoice.AccountId.HasValue)
        {
            accountName = invoice.FinancialAccount?.Name ?? "Unknown Account";
        }

        var categoryNames = invoice.SaleInvoiceItems.Where(ii => ii.CategoryId.HasValue)
            .Select(ii => ii.Category)
            .ToDictionary(c => c.Id, c => c.Name);

        string? employeeName = await context.Employees
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .Where(e => e.Id == invoice.EmployeeId)
            .Select(e => e.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        var itemResponses = items.Select(i => new StoreOperationItemResponse
        {
            Id = i.Id,
            Karat = (int)i.Karat,
            WeightInGrams = i.WeightInGrams,
            Equivalent21KWeightInGrams = i.Equivalent21KWeightInGrams,
            PricePerGram = i.PricePerGram,
            GoldAmount = i.GoldAmount,
            CategoryName = i.CategoryId.HasValue
                ? categoryNames.GetValueOrDefault(i.CategoryId.Value)
                : "Unknown Category"
        }).ToList();

        return new StoreOperationDetailResponse
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            OperationType = "Sale",
            OperationTypeLabel = "بيع",
            Date = invoice.Date,
            CounterpartyName = invoice.CustomerName,
            CounterpartyPhone = invoice.CustomerPhone,
            EmployeeName = employeeName ?? "Unknown Employee",
            Currency = invoice.Currency.ToString(),
            CurrencySymbol = GetCurrencySymbol(invoice.Currency.ToString()),
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid,
            RemainingBalance = invoice.RemainingBalance,
            PaymentMethod = invoice.PaymentMethod?.ToString(),
            PaymentMethodLabel = invoice.PaymentMethod.HasValue
                ? invoice.PaymentMethod!.Value.ToLabel()
                : "Unknown Payment Method",
            AccountId = invoice.AccountId,
            AccountName = accountName,
            Status = invoice.Status.ToString(),
            StatusLabel = invoice.Status.ToStatusLabel(),
            Notes = invoice.Notes,
            AccountNumber = invoice.CustomerAccountNumber,
            Items = itemResponses
        };
    }

    private async Task<Result<StoreOperationDetailResponse>> GetPurchaseDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        Domain.CustomerPurchases.CustomerPurchaseInvoice? invoice = await context.CustomerPurchaseInvoices
            .Include(p => p.Items).ThenInclude(i => i.Category)
            .Include(p => p.FinancialAccount)
            .AsNoTracking()
            .Where(p => p.TenantId == currentTenant.TenantId)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (invoice is null)
        {
            return
                Error.NotFound("StoreOperations.NotFound", "الفاتورة غير موجودة");
        }

        var items = invoice.Items.ToList();

        string? accountName = invoice.FinancialAccount?.Name ?? "Unknown Account";

        string? employeeName = await context.Employees
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .Where(e => e.Id == invoice.EmployeeId)
            .Select(e => e.FullName)
            .FirstOrDefaultAsync(cancellationToken);
        var itemResponses = items.Select(i => new StoreOperationItemResponse
        {
            Id = i.Id,
            Karat = (int)i.Karat,
            WeightInGrams = i.WeightInGrams,
            Equivalent21KWeightInGrams = i.Equivalent21KWeightInGrams,
            PricePerGram = i.PricePerGram,
            GoldAmount = i.GoldAmount,
            CategoryName = i.CategoryId.HasValue ? i.Category?.Name : "Unknown Category"

        }).ToList();

        return new StoreOperationDetailResponse
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            OperationType = "Buy",
            OperationTypeLabel = "شراء",
            Date = invoice.Date!.Value,
            CounterpartyName = invoice.SellerName,
            CounterpartyPhone = invoice.SellerPhone,
            EmployeeName = employeeName ?? "Unknown Employee",
            Currency = invoice.Currency.ToString(),
            CurrencySymbol = GetCurrencySymbol(invoice.Currency.ToString()),
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid,
            RemainingBalance = invoice.TotalAmount - invoice.AmountPaid,
            PaymentMethod = invoice.PaymentMethod.ToString(),
            PaymentMethodLabel = invoice.PaymentMethod.ToLabel(),
            AccountId = invoice.AccountId,
            AccountName = accountName,
            Status = null,
            StatusLabel = null,
            Notes = invoice.Notes,
            CounterpartyIdNumber = invoice.SellerIdNumber,
            CounterpartyYearOfBirth = invoice.SellerYearOfBirth,
            CounterpartyAddress = invoice.SellerAddress,
            AccountNumber = invoice.SellerAccountNumber,
            Items = itemResponses
        };
    }
}
