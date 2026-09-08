// <copyright file="CreateSalesInvoiceCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Services;
using Application.Abstractions.Subscriptions;
using Application.Common.Errors;
using Application.Common.Ledger;
using Domain.Catalog;
using Domain.Common;
using Domain.Debts;
using Domain.Employees;
using Domain.Finance;
using Domain.Inventory;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Result;

namespace Application.Features.SalesInvoices.Create;

internal sealed class CreateSalesInvoiceCommandHandler(
    IApplicationDbContext context,
    ISubscriptionGate subscriptionGate,
    IInvoiceNumberService invoiceNumberService,
    ILogger<CreateSalesInvoiceCommandHandler> logger)
    : ICommandHandler<CreateSalesInvoiceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSalesInvoiceCommand command, CancellationToken cancellationToken)
    {
        // Central quota gate (plan Phase 4 item 7): posted-invoices-per-period plan limit.
        Result<Success> quota = await subscriptionGate.EnsureCanPostInvoicesAsync(additionalInvoices: 1, cancellationToken);
        if (quota.IsError)
        {
            return quota.Errors;
        }

        if (command.Items.Count == 0)
        {
            return SalesInvoiceErrors.NoItems;
        }

        Guid? employeeId = command.EmployeeId;
        if (employeeId is not null && employeeId != Guid.Empty
            && !await context.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken))
        {
            return EmployeeErrors.NotFound(employeeId.Value);
        }

        Currency currency = Enum.Parse<Currency>(command.Currency);
        var invoiceId = Guid.CreateVersion7();

        // Preload the categories touched by this invoice so each category weight is
        // moved exactly once per line weight (single query instead of one per line).
        List<Guid> requestedCategoryIds = command.Items
            .Where(i => i.CategoryId.HasValue)
            .Select(i => i.CategoryId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, Category> categoriesById = requestedCategoryIds.Count > 0
            ? await context.Categories
                .Where(c => requestedCategoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, cancellationToken)
            : [];

        foreach (Guid categoryId in requestedCategoryIds)
        {
            if (!categoriesById.ContainsKey(categoryId))
            {
                return CategoryErrors.NotFound(categoryId);
            }
        }

        List<SalesInvoiceItem> items = [];

        foreach (SalesInvoiceItemDto item in command.Items)
        {
            var karat = (Karat)item.Karat;

            Result<SalesInvoiceItem> saleInvoceItemResult = SalesInvoiceItem.Create(invoiceId, item.CategoryId ?? Guid.Empty, karat, item.WeightInGrams, item.PricePerGram);
            if (saleInvoceItemResult.IsError)
            {
                return saleInvoceItemResult.Errors;
            }

            items.Add(saleInvoceItemResult.Value);
        }

        string invoiceNumber = await invoiceNumberService.AllocateAsync(
            InvoiceDocumentType.Sales, cancellationToken);

        decimal amountPaid;
        Guid accountId;
        PaymentMethod paymentMethod;

        if (command.PaymentLegs is { Count: > 0 } legs)
        {
            Result<PaymentLegResult> paymentResult = await context.ProcessPaymentLegsAsync(
                legs, currency, FinancialTransactionType.Inflow, FinancialReferenceType.SalesPayment,
                invoiceId, $"دفعة فاتورة {invoiceNumber} — {command.CustomerName}", cancellationToken);

            if (paymentResult.IsError)
            {
                return paymentResult.Errors;
            }

            amountPaid = paymentResult.Value.TotalBaseAmount;
            accountId = paymentResult.Value.PrimaryAccountId;
            paymentMethod = paymentResult.Value.PrimaryMethod;

            if (amountPaid > command.TotalAmount)
            {
                return SalesInvoiceErrors.InvalidPaymentAmount;
            }
        }
        else
        {
            amountPaid = command.AmountPaid;
            accountId = command.AccountId ?? Guid.Empty;
            paymentMethod = command.PaymentMethod.HasValue ? (PaymentMethod)command.PaymentMethod.Value : PaymentMethod.Cash;

            if (accountId != Guid.Empty
                && !await context.FinancialAccounts.AnyAsync(a => a.Id == accountId, cancellationToken))
            {
                return FinancialAccountErrors.NotFound(accountId);
            }

            if (command.AmountPaid > command.TotalAmount)
            {
                return SalesInvoiceErrors.InvalidPaymentAmount;
            }
        }

        decimal remainingBalance = command.TotalAmount - amountPaid;

        SalesInvoiceStatus status = remainingBalance switch
        {
            0 when amountPaid > 0 => SalesInvoiceStatus.Completed,
            > 0 when amountPaid > 0 => SalesInvoiceStatus.PartiallyPaid,
            > 0 when amountPaid == 0 => SalesInvoiceStatus.Draft,
            _ => SalesInvoiceStatus.Draft,
        };
        try
        {
            Result<SalesInvoice> invoiceResult = SalesInvoice.Create(
                invoiceId,
                invoiceNumber,
                command.CustomerName,
                command.CustomerPhone,
                command.Date,
                currency,
                command.TotalAmount,
                amountPaid,
                paymentMethod,
                status,
                command.BuyerAccountNumber,
                command.Notes,
                accountId,
                employeeId ?? Guid.Empty,
                items);
            if (invoiceResult.IsError)
            {
                return invoiceResult.Errors;
            }

            context.SalesInvoices.Add(invoiceResult.Value);
            await context.SalesInvoiceItems.AddRangeAsync(items, cancellationToken);

            foreach (IGrouping<Karat, SalesInvoiceItem> karatGroup in items.GroupBy(i => i.Karat))
            {
                decimal required = karatGroup.Sum(i => i.WeightInGrams);
                decimal available = await context.GetGoldStockAsync(karatGroup.Key, cancellationToken);

                if (required > available)
                {
                    return GoldInventoryErrors.InsufficientStock(karatGroup.Key, available, required);
                }
            }

            // Decrease each selected category weight by its line weight. The
            // GoldLedgerEntry Decrease loop below is the single general-store posting —
            // this update is weight-only on purpose (no double-count). Uncategorized
            // lines move the ledger only.
            foreach (SalesInvoiceItemDto line in command.Items)
            {
                if (!line.CategoryId.HasValue)
                {
                    continue;
                }

                Category category = categoriesById[line.CategoryId.Value];
                var lineKarat = (Karat)line.Karat;
                if (category.Karat.HasValue && category.Karat.Value != lineKarat)
                {
                    return Error.Validation(
                        "Sales.CategoryKaratMismatch",
                        $"عيار السطر ({lineKarat.KaratLabel()}) لا يطابق عيار التصنيف {category.Name} ({category.Karat.Value.KaratLabel()})");
                }

                Result<Updated> adjusted = category.DecreaseWeight(line.WeightInGrams);
                if (adjusted.IsError)
                {
                    return adjusted.Errors;
                }
            }

            foreach (SalesInvoiceItem item in items)
            {
                var itemId = Guid.CreateVersion7();
                Result<GoldLedgerEntry> goldLedgerEntryResult = GoldLedgerEntry.Create(
                    item.Karat,
                    item.WeightInGrams,
                    GoldMovementType.Decrease,
                    GoldReferenceType.Sale,
                    invoiceId,
                    $"فاتورة مبيعات {invoiceNumber}");
                if (goldLedgerEntryResult.IsError)
                {
                    return goldLedgerEntryResult.Errors;
                }

                context.GoldLedgerEntries.Add(goldLedgerEntryResult.Value);
            }

            if (command.PaymentLegs is not { Count: > 0 } && amountPaid > 0 && accountId != Guid.Empty)
            {
                Result<FinancialTransaction> financialTransactionResult = FinancialTransaction.Create(accountId, currency, amountPaid,
                    FinancialTransactionType.Inflow, FinancialReferenceType.SalesPayment,
                    invoiceId, $"دفعة فاتورة {invoiceNumber} — {command.CustomerName}");

                if (financialTransactionResult.IsError)
                {
                    return financialTransactionResult.Errors;
                }

                context.FinancialTransactions.Add(financialTransactionResult.Value);
            }

            if (remainingBalance > 0)
            {
                Result<Debt> debtResult = Debt.Create(command.CustomerName, command.CustomerPhone,
                    DebtDirection.Receivable, currency,
                    accountId,
                    $"باقي فاتورة {invoiceNumber} بتاريخ {command.Date:yyyy-MM-dd}");
                if (debtResult.IsError)
                {
                    return debtResult.Errors;
                }

                context.Debts.Add(debtResult.Value);

                Result<DebtLedgerEntry> debtLedgerEntryResult = DebtLedgerEntry.Create(debtResult.Value.Id, remainingBalance,
                    DebtBalanceMovementType.Increase,
                    $"رصيد متبقي من فاتورة {invoiceNumber}");
                if (debtLedgerEntryResult.IsError)
                {
                    return debtLedgerEntryResult.Errors;
                }

                context.DebtLedgerEntries.Add(debtLedgerEntryResult.Value);
            }

            await context.SaveChangesAsync(cancellationToken);
            return invoiceId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create sales invoice {InvoiceId}", invoiceId);
            return ApplicationErrors.DatabaseError;
        }
    }
}
