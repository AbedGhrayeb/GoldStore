using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Domain.Common;
using Domain.CustomerPurchases;
using Domain.Debts;
using Domain.Finance;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.CustomerPurchaseInvoices.Create;

internal sealed class CreateCustomerPurchaseInvoiceCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<CreateCustomerPurchaseInvoiceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateCustomerPurchaseInvoiceCommand command, CancellationToken cancellationToken)
    {
        if (command.Items.Count == 0)
        {
            return CustomerPurchaseInvoiceErrors.NoItems;
        }

        if (command.PaymentLegs is not { Count: > 0 } && command.AmountPaid > command.TotalAmount)
        {
            return CustomerPurchaseInvoiceErrors.InvalidPaymentAmount;
        }

        var invoiceId = Guid.CreateVersion7();

        try
        {
            Currency currency = Enum.Parse<Currency>(command.Currency);
            string invoiceNumber = await GenerateInvoiceNumberAsync(cancellationToken);

            decimal amountPaid;
            Guid accountId;
            PaymentMethod paymentMethod;

            if (command.PaymentLegs is { Count: > 0 } legs)
            {
                Result<PaymentLegResult> paymentResult = await context.ProcessPaymentLegsAsync(
                    legs, currency, FinancialTransactionType.Outflow, FinancialReferenceType.CustomerGoldPurchase,
                    invoiceId, $"دفعة فاتورة شراء ذهب {invoiceNumber} — {command.SellerName}", cancellationToken);

                if (paymentResult.IsError)
                {
                    return paymentResult.Errors;
                }

                amountPaid = paymentResult.Value.TotalBaseAmount;
                accountId = paymentResult.Value.PrimaryAccountId;
                paymentMethod = paymentResult.Value.PrimaryMethod;

                if (amountPaid > command.TotalAmount)
                {
                    return CustomerPurchaseInvoiceErrors.InvalidPaymentAmount;
                }
            }
            else
            {
                var legacyPaymentMethod = (PaymentMethod)command.PaymentMethod;
                FinancialAccountType expectedAccountType = legacyPaymentMethod == PaymentMethod.Cash
                    ? FinancialAccountType.Cash
                    : FinancialAccountType.Bank;

                FinancialAccount? account = await context.FinancialAccounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == command.AccountId && a.IsActive, cancellationToken);

                if (account is null)
                {
                    return CustomerPurchaseInvoiceErrors.AccountNotFound;
                }

                if (account.Currency != currency || account.AccountType != expectedAccountType)
                {
                    return CustomerPurchaseInvoiceErrors.AccountDoesNotMatchPayment;
                }

                amountPaid = command.AmountPaid;
                accountId = command.AccountId;
                paymentMethod = legacyPaymentMethod;
            }

            Guid userId = userContext.UserId;
            decimal remainingBalance = command.TotalAmount - amountPaid;
            List<CustomerPurchaseInvoiceItem> items = [];
            foreach (CustomerPurchaseInvoiceItemDto item in command.Items)
            {
                var karat = (Karat)item.Karat;

                Result<CustomerPurchaseInvoiceItem> itemResult =
                    CustomerPurchaseInvoiceItem.Create(item.CategoryId, karat, item.WeightInGrams, item.PricePerGram);
                if (itemResult.IsError)
                {
                    return itemResult.Errors;
                }
                items.Add(itemResult.Value);
            }
            Result<CustomerPurchaseInvoice> invoiceResult = CustomerPurchaseInvoice.Create(
                invoiceNumber,
                command.SellerName,
                command.SellerIdNumber,
                command.SellerPhone,
                command.SellerYearOfBirth,
                command.SellerAddress,
                command.EmployeeId,
                command.Date,
                currency,
                command.TotalAmount,
                amountPaid,
                paymentMethod,
                accountId,
                command.SellerAccountNumber,
                command.Notes,
                items

            );
            if (invoiceResult.IsError)
            {
                return invoiceResult.Errors;
            }
            context.CustomerPurchaseInvoices.Add(invoiceResult.Value);

            // Add financial transaction for the amount paid
            if (command.PaymentLegs is not { Count: > 0 } && amountPaid > 0)
            {
                decimal availableBalance = await context.GetAccountBalanceAsync(accountId, cancellationToken);

                if (amountPaid > availableBalance)
                {
                    return FinancialAccountErrors.InsufficientBalance(availableBalance, amountPaid);
                }

                Result<FinancialTransaction> financialTransactionResult = FinancialTransaction.Create(accountId, currency, amountPaid,
                    FinancialTransactionType.Outflow, FinancialReferenceType.CustomerGoldPurchase,
                    invoiceId, $"دفعة فاتورة شراء ذهب {invoiceNumber} — {command.SellerName}");
                if (financialTransactionResult.IsError)
                {
                    return financialTransactionResult.Errors;
                }
                context.FinancialTransactions.Add(financialTransactionResult.Value);
            }
            // Add debt record for the remaining balance if any
            if (remainingBalance > 0)
            {
                var debtId = Guid.CreateVersion7();
                Debt existingDebt = await context.Debts
                    .FirstOrDefaultAsync(d => d.Name == command.SellerName.Trim() && d.Direction == DebtDirection.Payable && d.Currency == currency, cancellationToken);
                if (existingDebt != null)
                {
                    debtId = existingDebt.Id;

                }
                else
                {
                    Result<Debt> debtResult = Debt.Create(command.SellerName, command.SellerPhone, DebtDirection.Payable, currency, command.AccountId,
                                          $"باقي فاتورة شراء ذهب {invoiceNumber} بتاريخ {command.Date:yyyy-MM-dd}");
                    if (debtResult.IsError)
                    { return debtResult.Errors; }
                    context.Debts.Add(debtResult.Value);
                }


                // Add debt ledger entry for the remaining balance

                Result<DebtLedgerEntry> debtLedgerEntryResult = DebtLedgerEntry.Create(debtId, remainingBalance, DebtBalanceMovementType.Increase,
                    $"رصيد متبقي من فاتورة شراء ذهب {invoiceNumber}");
                if (debtLedgerEntryResult.IsError)
                {
                    return debtLedgerEntryResult.Errors;
                }
                context.DebtLedgerEntries.Add(debtLedgerEntryResult.Value);
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return CustomerPurchaseInvoiceErrors.DatabaseError(ex);
        }

        return invoiceId;
    }

    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken cancellationToken)
    {
        string yearMonth = DateTime.UtcNow.ToString("yyyy-MM");

        int count = await context.CustomerPurchaseInvoices
            .CountAsync(i => i.InvoiceNumber.StartsWith($"PUR-{yearMonth}"), cancellationToken);

        return $"PUR-{yearMonth}-{count + 1:D4}";
    }
}
