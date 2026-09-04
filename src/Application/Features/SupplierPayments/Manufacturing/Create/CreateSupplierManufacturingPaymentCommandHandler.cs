using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Domain.Common;
using Domain.Finance;
using Domain.SupplierOperations;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
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
        Supplier? supplier = await context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == command.SupplierId, cancellationToken);

        if (supplier is null)
        {
            return SupplierErrors.NotFound(command.SupplierId);
        }

        if (!supplier.IsActive)
        {
            return SupplierErrors.SupplierNotActive;
        }

        FinancialAccount? account = await context.FinancialAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == command.AccountId, cancellationToken);

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

        if (command.PaymentLegs is { Count: > 0 } legs)
        {
            var paymentId = Guid.CreateVersion7();

            Result<PaymentLegResult> paymentResult = await context.ProcessPaymentLegsAsync(
                legs, currency, FinancialTransactionType.Outflow, FinancialReferenceType.SupplierManufacturingPayment,
                paymentId, command.Notes ?? "دفعة أجرة تصنيع", cancellationToken);

            if (paymentResult.IsError)
            {
                return paymentResult.Errors;
            }

            decimal totalBaseAmount = paymentResult.Value.TotalBaseAmount;
            Guid accountId = paymentResult.Value.PrimaryAccountId;

            if (totalBaseAmount > mfgBalance)
            {
                return SupplierErrors.InsufficientManufacturingBalance(currency, mfgBalance, totalBaseAmount);
            }

            var payment = SupplierManufacturingPayment.Create(command.SupplierId, accountId, totalBaseAmount, currency, command.Notes);
            context.SupplierManufacturingPayments.Add(payment);
            var manufacturingLedgerEntry = SupplierManufacturingLedgerEntry.Create(command.SupplierId, totalBaseAmount, currency,
                SupplierBalanceMovementType.Decrease, SupplierManufacturingReferenceType.SupplierManufacturingPayment, payment.Id, command.Notes);
            context.SupplierManufacturingLedgerEntries.Add(manufacturingLedgerEntry);

            await context.SaveChangesAsync(cancellationToken);

            return payment.Id;
        }

        if (command.Amount > mfgBalance)
        {
            return SupplierErrors.InsufficientManufacturingBalance(currency, mfgBalance, command.Amount);
        }

        decimal availableBalance = await context.GetAccountBalanceAsync(command.AccountId, cancellationToken);

        if (command.Amount > availableBalance)
        {
            return FinancialAccountErrors.InsufficientBalance(availableBalance, command.Amount);
        }

        var legacyPayment = SupplierManufacturingPayment.Create(command.SupplierId, command.AccountId, command.Amount, currency, command.Notes);
        context.SupplierManufacturingPayments.Add(legacyPayment);
        var legacyLedgerEntry = SupplierManufacturingLedgerEntry.Create(command.SupplierId, command.Amount, currency,
            SupplierBalanceMovementType.Decrease, SupplierManufacturingReferenceType.SupplierManufacturingPayment, legacyPayment.Id, command.Notes);
        context.SupplierManufacturingLedgerEntries.Add(legacyLedgerEntry);

        Result<FinancialTransaction> financialTransaction = FinancialTransaction.Create(command.AccountId, currency, command.Amount, FinancialTransactionType.Outflow,
            FinancialReferenceType.SupplierManufacturingPayment, legacyPayment.Id, command.Notes);

        if (financialTransaction.IsError)
        {
            return financialTransaction.Errors;
        }
        context.FinancialTransactions.Add(financialTransaction.Value);
        await context.SaveChangesAsync(cancellationToken);

        return legacyPayment.Id;
    }
}
