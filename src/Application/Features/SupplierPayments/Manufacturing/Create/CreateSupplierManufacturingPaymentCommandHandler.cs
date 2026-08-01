using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Domain.Common;
using Domain.Finance;
using Domain.SupplierOperations;
using Domain.Suppliers;
using SharedKernel;
using SharedKernel.Result;

namespace Application.SupplierPayments.Manufacturing.Create;

internal sealed class CreateSupplierManufacturingPaymentCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<CreateSupplierManufacturingPaymentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSupplierManufacturingPaymentCommand command, CancellationToken cancellationToken)
    {
        Supplier? supplier = await context.Suppliers.FindAsync([command.SupplierId], cancellationToken);

        if (supplier is null)
        {
            return SupplierErrors.NotFound(command.SupplierId);
        }

        if (!supplier.IsActive)
        {
            return SupplierErrors.SupplierNotActive;
        }

        FinancialAccount? account = await context.FinancialAccounts.FindAsync([command.AccountId], cancellationToken);

        if (account is null)
        {
            return FinancialAccountErrors.NotFound(command.AccountId);
        }

        if (!account.IsActive)
        {
            return FinancialAccountErrors.Inactive;
        }

        Currency currency = Enum.Parse<Currency>(command.Currency, ignoreCase: true);
        DateTime paymentDate = dateTimeProvider.UtcNow;
        Guid userId = userContext.UserId;

        decimal mfgBalance = await context.GetSupplierManufacturingBalanceAsync(command.SupplierId, currency, cancellationToken);

        if (command.Amount > mfgBalance)
        {
            return SupplierErrors.InsufficientManufacturingBalance(currency, mfgBalance, command.Amount);
        }

        decimal availableBalance = await context.GetAccountBalanceAsync(command.AccountId, cancellationToken);

        if (command.Amount > availableBalance)
        {
            return FinancialAccountErrors.InsufficientBalance(availableBalance, command.Amount);
        }

        var payment = SupplierManufacturingPayment.Create(command.SupplierId, command.AccountId, command.Amount, currency, command.Notes);
        context.SupplierManufacturingPayments.Add(payment);
        var manufacturingLedgerEntry = SupplierManufacturingLedgerEntry.Create(command.SupplierId, command.Amount, currency,
            SupplierBalanceMovementType.Decrease, SupplierManufacturingReferenceType.SupplierManufacturingPayment, payment.Id, command.Notes);
        context.SupplierManufacturingLedgerEntries.Add(manufacturingLedgerEntry);

        Result<FinancialTransaction> financialTransaction = FinancialTransaction.Create(command.AccountId, currency, command.Amount, FinancialTransactionType.Outflow,
            FinancialReferenceType.SupplierManufacturingPayment, payment.Id, command.Notes);

        if (financialTransaction.IsError)
        {
            return financialTransaction.Errors;
        }
        context.FinancialTransactions.Add(financialTransaction.Value);
        await context.SaveChangesAsync(cancellationToken);

        return payment.Id;
    }
}
