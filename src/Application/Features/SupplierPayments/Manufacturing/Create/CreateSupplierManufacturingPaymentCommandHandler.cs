using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Finance;
using Domain.SupplierOperations;
using Domain.Suppliers;
using SharedKernel;

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
            return Result.Failure<Guid>(SupplierErrors.NotFound(command.SupplierId));
        }

        if (!supplier.IsActive)
        {
            return Result.Failure<Guid>(Error.Problem("SupplierPayments.InactiveSupplier", "المورد غير نشط"));
        }

        FinancialAccount? account = await context.FinancialAccounts.FindAsync([command.AccountId], cancellationToken);

        if (account is null)
        {
            return Result.Failure<Guid>(Error.NotFound("FinancialAccount.NotFound", "حساب الدفع غير موجود"));
        }

        if (!account.IsActive)
        {
            return Result.Failure<Guid>(Error.Problem("FinancialAccount.Inactive", "حساب الدفع غير نشط"));
        }

        Currency currency = Enum.Parse<Currency>(command.Currency, ignoreCase: true);
        DateTime paymentDate = dateTimeProvider.UtcNow;
        Guid userId = userContext.UserId;

        var payment = new SupplierManufacturingPayment
        {
            Id = Guid.NewGuid(),
            SupplierId = command.SupplierId,
            AccountId = command.AccountId,
            Amount = command.Amount,
            Currency = currency,
            Date = paymentDate,
            Notes = command.Notes
        };

        context.SupplierManufacturingPayments.Add(payment);

        context.SupplierManufacturingLedgerEntries.Add(new SupplierManufacturingLedgerEntry
        {
            Id = Guid.NewGuid(),
            SupplierId = command.SupplierId,
            Amount = command.Amount,
            Currency = currency,
            MovementType = SupplierBalanceMovementType.Decrease,
            ReferenceType = SupplierManufacturingReferenceType.SupplierManufacturingPayment,
            ReferenceId = payment.Id,
            Date = paymentDate,
            Notes = command.Notes
        });

        context.FinancialTransactions.Add(new FinancialTransaction
        {
            Id = Guid.NewGuid(),
            AccountId = command.AccountId,
            Currency = currency,
            Amount = command.Amount,
            TransactionType = FinancialTransactionType.Outflow,
            ReferenceType = FinancialReferenceType.SupplierManufacturingPayment,
            ReferenceId = payment.Id,
            UserId = userId,
            Date = paymentDate,
            Notes = command.Notes
        });

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(payment.Id);
    }
}