using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
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
using SharedKernel.Result;

namespace Application.Features.SalesInvoices.Create;

internal sealed class CreateSalesInvoiceCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<CreateSalesInvoiceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSalesInvoiceCommand command, CancellationToken cancellationToken)
    {
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

        List<SalesInvoiceItem> Items = [];

        foreach (SalesInvoiceItemDto item in command.Items)
        {
            if (item.CategoryId is { } categoryId
                && !await context.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
            {
                return CategoryErrors.NotFound(categoryId);
            }

            var karat = (Karat)item.Karat;

            Result<SalesInvoiceItem> saleInvoceItemResult = SalesInvoiceItem.Create(invoiceId, item.CategoryId ?? Guid.Empty, karat, item.WeightInGrams, item.PricePerGram);
            if (saleInvoceItemResult.IsError)
            {
                return saleInvoceItemResult.Errors;
            }
            Items.Add(saleInvoceItemResult.Value);


        }

        string invoiceNumber = await GenerateInvoiceNumberAsync(cancellationToken);

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
            _ => SalesInvoiceStatus.Draft
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
                Items
                );
            if (invoiceResult.IsError)
            {
                return invoiceResult.Errors;
            }
            context.SalesInvoices.Add(invoiceResult.Value);
            await context.SalesInvoiceItems.AddRangeAsync(Items, cancellationToken);

            foreach (var karatGroup in Items.GroupBy(i => i.Karat))
            {
                decimal required = karatGroup.Sum(i => i.WeightInGrams);
                decimal available = await context.GetGoldStockAsync(karatGroup.Key, cancellationToken);

                if (required > available)
                {
                    return GoldInventoryErrors.InsufficientStock(karatGroup.Key, available, required);
                }
            }

            foreach (SalesInvoiceItem item in Items)
            {
                var itemId = Guid.CreateVersion7();
                Result<GoldLedgerEntry> goldLedgerEntryResult = GoldLedgerEntry.Create(
                    item.Karat,
                    item.WeightInGrams,
                    GoldMovementType.Decrease,
                    GoldReferenceType.Sale,
                    invoiceId,
                    $"فاتورة مبيعات {invoiceNumber}"
                );
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
            return ApplicationErrors.DatabaseError(ex);
        }
    }

    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken cancellationToken)
    {
        string yearMonth = DateTime.UtcNow.ToString("yyyy-MM");

        int count = await context.SalesInvoices
            .CountAsync(i => i.InvoiceNumber.StartsWith($"INV-{yearMonth}"), cancellationToken);

        return $"INV-{yearMonth}-{count + 1:D4}";
    }
}
