using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Users.Login;
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

        FinancialAccount? account = await context.FinancialAccounts
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

        if (account is null)
        {
            return SupplierFinancialErrors.AccountNotFound(command.AccountId);
        };

        if (!account.IsActive)

        {
            return SupplierFinancialErrors.AccountNotActive;
        };

        Currency currency = Enum.Parse<Currency>(command.Currency);
        var direction = (SupplierFinancialTransactionDirection)command.Direction;

        if (account.Currency != currency)
        { 
            return SupplierFinancialErrors.AccountCurrencyMismatch;
    }

        var transactionId = Guid.CreateVersion7();
        var supplierFinancialTransaction = SupplierFinancialTransaction.Create(command.SupplierId, direction, command.Amount, currency, command.AccountId, command.Notes);

        context.SupplierFinancialTransactions.Add(supplierFinancialTransaction);
        var supplierFinancialLedgerEntry = SupplierFinancialLedgerEntry.Create(supplierFinancialTransaction.Id, command.Amount, SupplierBalanceMovementType.Increase, command.Notes);
        context.SupplierFinancialLedgerEntries.Add(supplierFinancialLedgerEntry);

        FinancialTransactionType financialTransactionType = direction switch
        {
            SupplierFinancialTransactionDirection.FromSupplier => FinancialTransactionType.Inflow,
            SupplierFinancialTransactionDirection.ToSupplier => FinancialTransactionType.Outflow,
            _ => FinancialTransactionType.Inflow
        };
        Result<FinancialTransaction> financialTransaction = FinancialTransaction.Create(command.AccountId,currency, command.Amount,
            financialTransactionType, FinancialReferenceType.SupplierLoan, 
            supplierFinancialTransaction.Id, command.Notes);

        if(financialTransaction.IsError)
        {
            return financialTransaction.Errors;
        }

        context.FinancialTransactions.Add(financialTransaction.Value);

        await context.SaveChangesAsync(cancellationToken);

        return transactionId;
    }

}
