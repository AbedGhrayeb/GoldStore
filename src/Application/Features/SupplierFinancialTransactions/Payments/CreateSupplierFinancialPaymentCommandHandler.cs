using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Finance;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

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
            return Result.Failure<Guid>(SupplierFinancialErrors.NotFound(command.TransactionId));

        FinancialAccount? account = await context.FinancialAccounts
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
            return Result.Failure<Guid>(SupplierFinancialErrors.AccountNotFound(command.AccountId));

        if (!account.IsActive)
            return Result.Failure<Guid>(SupplierFinancialErrors.AccountNotActive);

        if (account.Currency != transaction.Currency)
            return Result.Failure<Guid>(SupplierFinancialErrors.AccountCurrencyMismatch);

        if (command.Amount <= 0)
            return Result.Failure<Guid>(SupplierFinancialErrors.PaymentAmountMustBePositive);

        decimal outstandingBalance = await context.SupplierFinancialLedgerEntries
            .Where(e => e.SupplierFinancialTransactionId == command.TransactionId)
            .SumAsync(e => e.MovementType == SupplierBalanceMovementType.Increase
                ? e.Amount
                : -e.Amount, cancellationToken);

        if (command.Amount > outstandingBalance)
            return Result.Failure<Guid>(SupplierFinancialErrors.PaymentExceedsBalance(command.Amount, outstandingBalance));

        var paymentId = Guid.CreateVersion7();

        context.SupplierFinancialPayments.Add(new SupplierFinancialPayment
        {
            Id = paymentId,
            SupplierFinancialTransactionId = command.TransactionId,
            Amount = command.Amount,
            AccountId = command.AccountId,
            Date = command.Date,
            Notes = command.Notes
        });

        context.SupplierFinancialLedgerEntries.Add(new SupplierFinancialLedgerEntry
        {
            Id = Guid.CreateVersion7(),
            SupplierFinancialTransactionId = command.TransactionId,
            Amount = command.Amount,
            MovementType = SupplierBalanceMovementType.Decrease,
            Date = command.Date,
            Notes = command.Notes ?? "دفعة على سلفة"
        });

        var financialTransactionType = transaction.Direction switch
        {
            SupplierFinancialTransactionDirection.FromSupplier => FinancialTransactionType.Outflow,
            SupplierFinancialTransactionDirection.ToSupplier => FinancialTransactionType.Inflow,
            _ => FinancialTransactionType.Outflow
        };

        context.FinancialTransactions.Add(new FinancialTransaction
        {
            Id = Guid.CreateVersion7(),
            AccountId = command.AccountId,
            Currency = account.Currency,
            Amount = command.Amount,
            TransactionType = financialTransactionType,
            ReferenceType = FinancialReferenceType.SupplierLoan,
            ReferenceId = command.TransactionId,
            Date = command.Date,
            Notes = command.Notes ?? "دفعة على سلفة"
        });

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(paymentId);
    }
}
