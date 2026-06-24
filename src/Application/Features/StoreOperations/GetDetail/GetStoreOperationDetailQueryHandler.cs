using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.StoreOperations.Shared;
using Domain.CustomerPurchases;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.StoreOperations.GetDetail;

internal sealed class GetStoreOperationDetailQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetStoreOperationDetailQuery, StoreOperationDetailResponse>
{
    private static string GetStatusLabel(SalesInvoiceStatus status) => status switch
    {
        SalesInvoiceStatus.Draft => "مسودة",
        SalesInvoiceStatus.Completed => "مكتملة",
        SalesInvoiceStatus.PartiallyPaid => "مدفوعة جزئياً",
        SalesInvoiceStatus.Cancelled => "ملغاة",
        _ => status.ToString()
    };

    private static string GetPaymentMethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "نقدي",
        PaymentMethod.Bank => "مصرفي",
        _ => method.ToString()
    };

    private static string GetCurrencySymbol(string currency) => currency switch
    {
        "Jod" => "د.أ",
        "Usd" => "$",
        "Ils" => "₪",
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

        return Result.Failure<StoreOperationDetailResponse>(
            Error.Failure("StoreOperations.InvalidType", "نوع العملية غير صالح"));
    }

    private async Task<Result<StoreOperationDetailResponse>> GetSaleDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        Domain.Sales.SalesInvoice? invoice = await context.SalesInvoices
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (invoice is null)
        {
            return Result.Failure<StoreOperationDetailResponse>(
                Error.NotFound("StoreOperations.NotFound", "الفاتورة غير موجودة"));
        }

        List<SalesInvoiceItem> items = await context.SalesInvoiceItems
            .AsNoTracking()
            .Where(i => i.SalesInvoiceId == invoice.Id)
            .ToListAsync(cancellationToken);

        string? accountName = null;
        if (invoice.AccountId.HasValue)
        {
            accountName = await context.FinancialAccounts
                .AsNoTracking()
                .Where(a => a.Id == invoice.AccountId.Value)
                .Select(a => a.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var categoryIds = items.Select(i => i.CategoryId).Where(c => c.HasValue).Select(c => c!.Value).ToList();
        Dictionary<Guid, string> categoryNames = await context.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

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
                : null
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
            EmployeeName = invoice.SellerName,
            Currency = invoice.Currency.ToString(),
            CurrencySymbol = GetCurrencySymbol(invoice.Currency.ToString()),
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid,
            RemainingBalance = invoice.RemainingBalance,
            PaymentMethod = invoice.PaymentMethod?.ToString(),
            PaymentMethodLabel = invoice.PaymentMethod.HasValue
                ? GetPaymentMethodLabel(invoice.PaymentMethod.Value)
                : null,
            AccountId = invoice.AccountId,
            AccountName = accountName,
            Status = invoice.Status.ToString(),
            StatusLabel = GetStatusLabel(invoice.Status),
            Notes = invoice.Notes,
            AccountNumber = invoice.BuyerAccountNumber,
            Items = itemResponses
        };
    }

    private async Task<Result<StoreOperationDetailResponse>> GetPurchaseDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        Domain.CustomerPurchases.CustomerPurchaseInvoice? invoice = await context.CustomerPurchaseInvoices
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (invoice is null)
        {
            return Result.Failure<StoreOperationDetailResponse>(
                Error.NotFound("StoreOperations.NotFound", "الفاتورة غير موجودة"));
        }

        List<CustomerPurchaseInvoiceItem> items = await context.CustomerPurchaseInvoiceItems
            .AsNoTracking()
            .Where(i => i.CustomerPurchaseInvoiceId == invoice.Id)
            .ToListAsync(cancellationToken);

        string? accountName = await context.FinancialAccounts
            .AsNoTracking()
            .Where(a => a.Id == invoice.AccountId)
            .Select(a => a.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var categoryIds = items.Select(i => i.CategoryId).Where(c => c.HasValue).Select(c => c!.Value).ToList();
        Dictionary<Guid, string> categoryNames = await context.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

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
                : null
        }).ToList();

        return new StoreOperationDetailResponse
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            OperationType = "Buy",
            OperationTypeLabel = "شراء",
            Date = invoice.Date,
            CounterpartyName = invoice.SellerName,
            CounterpartyPhone = invoice.SellerPhone,
            EmployeeName = invoice.BuyerName,
            Currency = invoice.Currency.ToString(),
            CurrencySymbol = GetCurrencySymbol(invoice.Currency.ToString()),
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid,
            RemainingBalance = invoice.TotalAmount - invoice.AmountPaid,
            PaymentMethod = invoice.PaymentMethod.ToString(),
            PaymentMethodLabel = GetPaymentMethodLabel(invoice.PaymentMethod),
            AccountId = invoice.AccountId,
            AccountName = accountName,
            Status = null,
            StatusLabel = null,
            Notes = invoice.Notes,
            CounterpartyIdNumber = invoice.SellerIdNumber,
            CounterpartyYearOfBirth = invoice.SeelerYearOfBirth,
            CounterpartyAddress = invoice.SellerAddress,
            AccountNumber = invoice.SellerAccountNumber,
            Items = itemResponses
        };
    }
}
