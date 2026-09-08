// <copyright file="CreateSupplierFinancialTransactionCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Domain.Common;
using Domain.Finance;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Result;

namespace Application.Features.SupplierFinancialTransactions.Create;

internal sealed class CreateSupplierFinancialTransactionCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<CreateSupplierFinancialTransactionCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSupplierFinancialTransactionCommand command, CancellationToken cancellationToken)
    {
        Supplier? supplier = await context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == command.SupplierId, cancellationToken);

        if (supplier is null)
        {
            return SupplierFinancialErrors.SupplierNotFound(command.SupplierId);
        }

        if (!supplier.IsActive)
        {
            return SupplierFinancialErrors.SupplierNotActive;
        }

        if (!Enum.TryParse(command.Currency, ignoreCase: true, out Currency currency)
            || !Enum.IsDefined(currency))
        {
            return Error.Validation("SupplierFinancial.InvalidCurrency", "العملة غير صالحة");
        }

        var direction = (SupplierFinancialTransactionDirection)command.Direction;

        FinancialTransactionType financialTransactionType = direction switch
        {
            SupplierFinancialTransactionDirection.FromSupplier => FinancialTransactionType.Inflow,
            SupplierFinancialTransactionDirection.ToSupplier => FinancialTransactionType.Outflow,
            _ => FinancialTransactionType.Inflow,
        };

        var transactionId = Guid.CreateVersion7();

        decimal amount;
        Guid accountId;

        if (command.PaymentLegs is { Count: > 0 } legs)
        {
            Result<PaymentLegResult> paymentResult = await context.ProcessPaymentLegsAsync(
                legs, currency, financialTransactionType, FinancialReferenceType.SupplierLoan,
                transactionId, command.Notes ?? "سلفة مورد", cancellationToken);

            if (paymentResult.IsError)
            {
                return paymentResult.Errors;
            }

            amount = paymentResult.Value.TotalBaseAmount;
            accountId = paymentResult.Value.PrimaryAccountId;
        }
        else
        {
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

            if (account.Currency != currency)
            {
                return SupplierFinancialErrors.AccountCurrencyMismatch;
            }

            amount = command.Amount;
            accountId = command.AccountId;

            if (financialTransactionType == FinancialTransactionType.Outflow)
            {
                decimal availableBalance = await context.GetAccountBalanceAsync(accountId, cancellationToken);

                if (amount > availableBalance)
                {
                    return FinancialAccountErrors.InsufficientBalance(availableBalance, amount);
                }
            }
        }

        var supplierFinancialTransaction = SupplierFinancialTransaction.Create(
            transactionId,
            command.SupplierId,
            direction,
            amount,
            currency,
            accountId,
            command.Notes);

        context.SupplierFinancialTransactions.Add(supplierFinancialTransaction);
        var supplierFinancialLedgerEntry = SupplierFinancialLedgerEntry.Create(supplierFinancialTransaction.Id, amount, SupplierBalanceMovementType.Increase, command.Notes);
        context.SupplierFinancialLedgerEntries.Add(supplierFinancialLedgerEntry);

        if (command.PaymentLegs is not { Count: > 0 })
        {
            Result<FinancialTransaction> financialTransaction = FinancialTransaction.Create(accountId, currency, amount,
                financialTransactionType, FinancialReferenceType.SupplierLoan,
                supplierFinancialTransaction.Id, command.Notes);

            if (financialTransaction.IsError)
            {
                return financialTransaction.Errors;
            }

            context.FinancialTransactions.Add(financialTransaction.Value);
        }

        await context.SaveChangesAsync(cancellationToken);

        return supplierFinancialTransaction.Id;
    }
}
