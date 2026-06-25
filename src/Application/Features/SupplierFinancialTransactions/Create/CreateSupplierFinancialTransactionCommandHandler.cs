using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Finance;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

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
            return Result.Failure<Guid>(SupplierFinancialErrors.SupplierNotFound(command.SupplierId));

        if (!supplier.IsActive)
            return Result.Failure<Guid>(SupplierFinancialErrors.SupplierNotActive);

        FinancialAccount? account = await context.FinancialAccounts
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
            return Result.Failure<Guid>(SupplierFinancialErrors.AccountNotFound(command.AccountId));

        if (!account.IsActive)
            return Result.Failure<Guid>(SupplierFinancialErrors.AccountNotActive);

        var currency = Enum.Parse<Currency>(command.Currency);
        var direction = (SupplierFinancialTransactionDirection)command.Direction;

        if (account.Currency != currency)
            return Result.Failure<Guid>(SupplierFinancialErrors.AccountCurrencyMismatch);

        var transactionId = Guid.CreateVersion7();

        var transaction = new SupplierFinancialTransaction
        {
            Id = transactionId,
            SupplierId = command.SupplierId,
            Direction = direction,
            Amount = command.Amount,
            Currency = currency,
            AccountId = command.AccountId,
            Notes = command.Notes,
            CreatedAt = command.Date
        };

        context.SupplierFinancialTransactions.Add(transaction);

        context.SupplierFinancialLedgerEntries.Add(new SupplierFinancialLedgerEntry
        {
            Id = Guid.CreateVersion7(),
            SupplierFinancialTransactionId = transactionId,
            Amount = command.Amount,
            MovementType = SupplierBalanceMovementType.Increase,
            Date = command.Date,
            Notes = command.Notes ?? GetDirectionLabel(direction)
        });

        var financialTransactionType = direction switch
        {
            SupplierFinancialTransactionDirection.FromSupplier => FinancialTransactionType.Inflow,
            SupplierFinancialTransactionDirection.ToSupplier => FinancialTransactionType.Outflow,
            _ => FinancialTransactionType.Inflow
        };

        context.FinancialTransactions.Add(new FinancialTransaction
        {
            Id = Guid.CreateVersion7(),
            AccountId = command.AccountId,
            Currency = account.Currency,
            Amount = command.Amount,
            TransactionType = financialTransactionType,
            ReferenceType = FinancialReferenceType.SupplierLoan,
            ReferenceId = transactionId,
            Date = command.Date,
            Notes = command.Notes ?? $"{GetDirectionLabel(direction)} — {supplier.Name}"
        });

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(transactionId);
    }

    private static string GetDirectionLabel(SupplierFinancialTransactionDirection direction) => direction switch
    {
        SupplierFinancialTransactionDirection.FromSupplier => "سلفة من مورد",
        SupplierFinancialTransactionDirection.ToSupplier => "سلفة لمورد",
        _ => "معاملة مالية"
    };
}
