using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Inventory;
using Domain.SupplierOperations;
using Domain.Suppliers;
using SharedKernel;

namespace Application.SupplierDeliveries.Create;

internal sealed class CreateSupplierDeliveryCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<CreateSupplierDeliveryCommand, string>
{
    public async Task<Result<string>> Handle(CreateSupplierDeliveryCommand command, CancellationToken cancellationToken)
    {
        Supplier? supplier = await context.Suppliers.FindAsync([command.SupplierId], cancellationToken);
        if (supplier is null)
        {
            return Result.Failure<string>(SupplierErrors.NotFound(command.SupplierId));
        }

        if (!supplier.IsActive)
        {
            return Result.Failure<string>(Error.Problem("Suppliers.Inactive", "المورد غير نشط"));
        }

        Currency currency = Enum.Parse<Currency>(command.ManufacturingFeeCurrency, ignoreCase: true);
        var deliveryIds = new List<Guid>();
        DateTime deliveryDate = DateTime.UtcNow;
        Guid userId = userContext.UserId;
        decimal totalEquivalent21K = 0M;
        foreach (DeliveryLineDto line in command.Lines)
        {
            var karat = (Karat)line.Karat;
            decimal equivalent21K = GoldWeight.CalculateEquivalent21KWeight(line.WeightInGrams, karat);
            totalEquivalent21K += equivalent21K;
            var delivery = new SupplierDelivery
            {
                Id = Guid.CreateVersion7(),
                SupplierId = command.SupplierId,
                Karat = karat,
                WeightInGrams = line.WeightInGrams,
                Equivalent21KWeightInGrams = equivalent21K,
                ManufacturingFeePerGram = command.ManufacturingFeePerGram,
                TotalManufacturingFee = command.ManufacturingFeePerGram * equivalent21K,
                ManufacturingFeeCurrency = currency,
                Date = deliveryDate,
                Notes = command.Notes
            };

            context.SupplierDeliveries.Add(delivery);
            deliveryIds.Add(delivery.Id);
            //to calc supplier balance
            context.SupplierGoldLedgerEntries.Add(new SupplierGoldLedgerEntry
            {
                Id = Guid.CreateVersion7(),
                SupplierId = command.SupplierId,
                Karat = karat,
                WeightInGrams = line.WeightInGrams,
                Equivalent21KWeightInGrams = equivalent21K,
                MovementType = SupplierBalanceMovementType.Increase,
                ReferenceType = SupplierGoldReferenceType.SupplierDelivery,
                ReferenceId = delivery.Id,
                Date = deliveryDate,
                Notes = command.Notes
            });
            //to calc our store balance 
            context.GoldLedgerEntries.Add(new GoldLedgerEntry
            {
                Id = Guid.CreateVersion7(),
                Karat = karat,
                WeightInGrams = line.WeightInGrams,
                Equivalent21KWeightInGrams = equivalent21K,
                MovementType = GoldMovementType.Increase,
                ReferenceType = GoldReferenceType.SupplierDelivery,
                ReferenceId = delivery.Id,
                UserId = userId,
                Date = deliveryDate,
                Notes = command.Notes
            });
        }

        //decimal totalEquivalent21K = command.Lines.Sum(l =>
        //    GoldWeight.CalculateEquivalent21KWeight(l.WeightInGrams, (Karat)l.Karat));
        decimal totalMfgFee = command.ManufacturingFeePerGram * totalEquivalent21K;

        if (totalMfgFee > 0)
        {
            context.SupplierManufacturingLedgerEntries.Add(new SupplierManufacturingLedgerEntry
            {
                Id = Guid.CreateVersion7(),
                SupplierId = command.SupplierId,
                Amount = totalMfgFee,
                Currency = currency,
                MovementType = SupplierBalanceMovementType.Increase,
                ReferenceType = SupplierManufacturingReferenceType.SupplierDelivery,
                ReferenceId = deliveryIds.First(),
                Date = deliveryDate,
                Notes = command.Notes
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(string.Join(",", deliveryIds));
    }
}
