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
        { return SupplierFinancialErrors.NotFound(command.TransactionId); }

        FinancialAccount? account = await context.FinancialAccounts
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
        { return SupplierFinancialErrors.AccountNotFound(command.AccountId); }

        if (!account.IsActive)
        { return SupplierFinancialErrors.AccountNotActive; }

        if (account.Currency != transaction.Currency)
        { return SupplierFinancialErrors.AccountCurrencyMismatch; }

        if (command.Amount <= 0)
        { return SupplierFinancialErrors.PaymentAmountMustBePositive; }

        decimal outstandingBalance = await context.SupplierFinancialLedgerEntries
            .Where(e => e.SupplierFinancialTransactionId == command.TransactionId)
            .SumAsync(e => e.MovementType == SupplierBalanceMovementType.Increase
                ? e.Amount
                : -e.Amount, cancellationToken);

        if (command.Amount > outstandingBalance)
        { return SupplierFinancialErrors.PaymentExceedsBalance(command.Amount, outstandingBalance); }


        Result<SupplierFinancialPayment> paymentResult = SupplierFinancialPayment.Create(command.TransactionId, command.Amount,
            command.AccountId, command.Notes ?? "دفعة على سلفة");
        if (paymentResult.IsError)
        {
            return paymentResult.Errors;
        }
        context.SupplierFinancialPayments.Add(paymentResult.Value);

        var supplierFinancialLedgerEntry = SupplierFinancialLedgerEntry.Create(command.TransactionId, command.Amount,
            SupplierBalanceMovementType.Decrease, command.Notes ?? "دفعة على سلفة");

        context.SupplierFinancialLedgerEntries.Add(supplierFinancialLedgerEntry);


        FinancialTransactionType financialTransactionType = transaction.Direction switch
        {
            SupplierFinancialTransactionDirection.FromSupplier => FinancialTransactionType.Outflow,
            SupplierFinancialTransactionDirection.ToSupplier => FinancialTransactionType.Inflow,
            _ => FinancialTransactionType.Outflow
        };

        if (financialTransactionType == FinancialTransactionType.Outflow)
        {
            decimal availableBalance = await context.GetAccountBalanceAsync(account.Id, cancellationToken);

            if (command.Amount > availableBalance)
            {
                return FinancialAccountErrors.InsufficientBalance(availableBalance, command.Amount);
            }
        }

        Result<FinancialTransaction> financialTransaction = FinancialTransaction.Create(
            account.Id,
            account.Currency,
            command.Amount,
            financialTransactionType,
            FinancialReferenceType.SupplierLoan,
            command.TransactionId,
            command.Notes ?? "دفعة على سلفة");
        if (financialTransaction.IsError)
        {
            return financialTransaction.Errors;
        }
        context.FinancialTransactions.Add(financialTransaction.Value);

        await context.SaveChangesAsync(cancellationToken);

        return paymentResult.Value.Id;
    }
}
