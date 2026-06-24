using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Inventory;
using Domain.SupplierOperations;
using Domain.Suppliers;
using SharedKernel;

namespace Application.SupplierPayments.ScrapGold.Create;

internal sealed class CreateSupplierScrapGoldPaymentCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<CreateSupplierScrapGoldPaymentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSupplierScrapGoldPaymentCommand command, CancellationToken cancellationToken)
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

        var karat = (Karat)command.Karat;
        decimal equivalent21K = GoldWeight.CalculateEquivalent21KWeight(command.WeightInGrams, karat);
        DateTime paymentDate = dateTimeProvider.UtcNow;
        Guid userId = userContext.UserId;

        var payment = new SupplierScrapGoldPayment
        {
            Id = Guid.NewGuid(),
            SupplierId = command.SupplierId,
            Karat = karat,
            WeightInGrams = command.WeightInGrams,
            Equivalent21KWeightInGrams = equivalent21K,
            Date = paymentDate,
            Notes = command.Notes
        };

        context.SupplierScrapGoldPayments.Add(payment);

        context.SupplierGoldLedgerEntries.Add(new SupplierGoldLedgerEntry
        {
            Id = Guid.NewGuid(),
            SupplierId = command.SupplierId,
            Karat = karat,
            WeightInGrams = command.WeightInGrams,
            Equivalent21KWeightInGrams = equivalent21K,
            MovementType = SupplierBalanceMovementType.Decrease,
            ReferenceType = SupplierGoldReferenceType.SupplierScrapPayment,
            ReferenceId = payment.Id,
            Date = paymentDate,
            Notes = command.Notes
        });

        context.GoldLedgerEntries.Add(new GoldLedgerEntry
        {
            Id = Guid.NewGuid(),
            Karat = karat,
            WeightInGrams = command.WeightInGrams,
            Equivalent21KWeightInGrams = equivalent21K,
            MovementType = GoldMovementType.Decrease,
            ReferenceType = GoldReferenceType.SupplierScrapPayment,
            ReferenceId = payment.Id,
            UserId = userId,
            Date = paymentDate,
            Notes = command.Notes
        });

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(payment.Id);
    }
}