// <copyright file="CreateSupplierFinancialPaymentCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Domain.Finance;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.SupplierFinancialTransactions.Payments;

internal sealed class CreateSupplierFinancialPaymentCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<CreateSupplierFinancialPaymentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSupplierFinancialPaymentCommand command, CancellationToken cancellationToken)
    {
        SupplierFinancialTransaction? transaction = await context.SupplierFinancialTransactions
            .FirstOrDefaultAsync(t => t.Id == command.TransactionId, cancellationToken);

        if (transaction is null)
        {
            return SupplierFinancialErrors.NotFound(command.TransactionId);
        }

        FinancialTransactionType financialTransactionType = transaction.Direction switch
        {
            SupplierFinancialTransactionDirection.FromSupplier => FinancialTransactionType.Outflow,
            SupplierFinancialTransactionDirection.ToSupplier => FinancialTransactionType.Inflow,
            _ => FinancialTransactionType.Outflow,
        };

        decimal outstandingBalance = await context.SupplierFinancialLedgerEntries
            .Where(e => e.SupplierFinancialTransactionId == command.TransactionId)
            .SumAsync(
                e => e.MovementType == SupplierBalanceMovementType.Increase
                ? e.Amount
                : -e.Amount, cancellationToken);

        decimal amount;
        Guid accountId;

        if (command.PaymentLegs is { Count: > 0 } legs)
        {
            Result<PaymentLegResult> paymentResult = await context.ProcessPaymentLegsAsync(
                legs, transaction.Currency, financialTransactionType, FinancialReferenceType.SupplierLoan,
                command.TransactionId, command.Notes ?? "دفعة على سلفة", cancellationToken);

            if (paymentResult.IsError)
            {
                return paymentResult.Errors;
            }

            amount = paymentResult.Value.TotalBaseAmount;
            accountId = paymentResult.Value.PrimaryAccountId;

            if (amount > outstandingBalance)
            {
                return SupplierFinancialErrors.PaymentExceedsBalance(amount, outstandingBalance);
            }
        }
        else
        {
            if (command.Amount <= 0)
            {
                return SupplierFinancialErrors.PaymentAmountMustBePositive;
            }

            if (command.Amount > outstandingBalance)
            {
                return SupplierFinancialErrors.PaymentExceedsBalance(command.Amount, outstandingBalance);
            }

            FinancialAccount? account = await context.FinancialAccounts
                .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

            if (account is null)
            {
                return SupplierFinancialErrors.AccountNotFound(command.AccountId);
            }

            if (!account.IsActive)
            {
                return SupplierFinancialErrors.AccountNotActive;
            }

            if (account.Currency != transaction.Currency)
            {
                return SupplierFinancialErrors.AccountCurrencyMismatch;
            }

            amount = command.Amount;
            accountId = command.AccountId;
        }

        Result<SupplierFinancialPayment> paymentResultValue = SupplierFinancialPayment.Create(command.TransactionId, amount,
            accountId, command.Notes ?? "دفعة على سلفة");
        if (paymentResultValue.IsError)
        {
            return paymentResultValue.Errors;
        }

        context.SupplierFinancialPayments.Add(paymentResultValue.Value);

        var supplierFinancialLedgerEntry = SupplierFinancialLedgerEntry.Create(command.TransactionId, amount,
            SupplierBalanceMovementType.Decrease, command.Notes ?? "دفعة على سلفة");

        context.SupplierFinancialLedgerEntries.Add(supplierFinancialLedgerEntry);

        if (financialTransactionType == FinancialTransactionType.Outflow)
        {
            decimal availableBalance = await context.GetAccountBalanceAsync(accountId, cancellationToken);

            if (amount > availableBalance)
            {
                return FinancialAccountErrors.InsufficientBalance(availableBalance, amount);
            }
        }

        if (command.PaymentLegs is not { Count: > 0 })
        {
            Result<FinancialTransaction> financialTransaction = FinancialTransaction.Create(
                accountId,
                transaction.Currency,
                amount,
                financialTransactionType,
                FinancialReferenceType.SupplierLoan,
                command.TransactionId,
                command.Notes ?? "دفعة على سلفة");
            if (financialTransaction.IsError)
            {
                return financialTransaction.Errors;
            }

            context.FinancialTransactions.Add(financialTransaction.Value);
        }

        await context.SaveChangesAsync(cancellationToken);

        return paymentResultValue.Value.Id;
    }
}
