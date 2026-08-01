using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Errors;
using Domain.Common;
using Domain.Debts;
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

        Currency currency = Enum.Parse<Currency>(command.Currency);
        var invoiceId = Guid.CreateVersion7();

        List<SalesInvoiceItem> Items = [];

        foreach (SalesInvoiceItemDto item in command.Items)
        {
            var karat = (Karat)item.Karat;

            Result<SalesInvoiceItem> saleInvoceItemResult = SalesInvoiceItem.Create(invoiceId, item.CategoryId ?? Guid.Empty, karat, item.WeightInGrams, item.PricePerGram);
            if (saleInvoceItemResult.IsError)
            {
                return saleInvoceItemResult.Errors;
            }
            Items.Add(saleInvoceItemResult.Value);


        }

        decimal remainingBalance = command.TotalAmount - command.AmountPaid;

        if (command.AmountPaid > command.TotalAmount)
        {
            return SalesInvoiceErrors.InvalidPaymentAmount;
        }

        string invoiceNumber = await GenerateInvoiceNumberAsync(cancellationToken);


        SalesInvoiceStatus status = remainingBalance switch
        {
            0 when command.AmountPaid > 0 => SalesInvoiceStatus.Completed,
            > 0 when command.AmountPaid > 0 => SalesInvoiceStatus.PartiallyPaid,
            > 0 when command.AmountPaid == 0 => SalesInvoiceStatus.Draft,
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
                command.AmountPaid,
                command.PaymentMethod.HasValue ? (PaymentMethod)command.PaymentMethod.Value : PaymentMethod.Cash,
                status,
                command.BuyerAccountNumber,
                command.Notes,
                command.AccountId ?? Guid.Empty,
                command.EmployeeId ?? Guid.Empty,
                Items
                );
            if (invoiceResult.IsError)
            {
                return invoiceResult.Errors;
            }
            context.SalesInvoices.Add(invoiceResult.Value);
            await context.SalesInvoiceItems.AddRangeAsync(Items, cancellationToken);
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
            if (command.AmountPaid > 0 && command.AccountId.HasValue)
            {
                Result<FinancialTransaction> financialTransactionResult = FinancialTransaction.Create(command.AccountId ?? Guid.Empty, currency, command.AmountPaid,
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
                    command.AccountId ?? Guid.Empty,
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
