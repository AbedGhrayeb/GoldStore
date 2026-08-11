using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Inventory;
using Domain.SupplierOperations;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.SupplierDeliveries.Create;

internal sealed class CreateSupplierDeliveryCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<CreateSupplierDeliveryCommand, string>
{
    public async Task<Result<string>> Handle(CreateSupplierDeliveryCommand command, CancellationToken cancellationToken)
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
            var delivery = SupplierDelivery.Create(
                command.SupplierId,
                karat,
                line.WeightInGrams,
                command.ManufacturingFeePerGram,
                currency,
                command.Notes);


            context.SupplierDeliveries.Add(delivery);
            deliveryIds.Add(delivery.Id);
            //to calc supplier balance
            var supplierGoldLedgerEntry = SupplierGoldLedgerEntry.Create(command.SupplierId, karat, line.WeightInGrams,
                SupplierBalanceMovementType.Increase, SupplierGoldReferenceType.SupplierDelivery, delivery.Id, command.Notes);
            context.SupplierGoldLedgerEntries.Add(supplierGoldLedgerEntry);

            //to calc our store balance 
            Result<GoldLedgerEntry> goldLedgerEntry = GoldLedgerEntry.Create(karat, line.WeightInGrams,
                GoldMovementType.Increase, GoldReferenceType.SupplierDelivery, delivery.Id, command.Notes);
            if (goldLedgerEntry.IsError)
            {
                return goldLedgerEntry.Errors;
            }
            context.GoldLedgerEntries.Add(goldLedgerEntry.Value);

        }

        //decimal totalEquivalent21K = command.Lines.Sum(l =>
        decimal totalMfgFee = command.ManufacturingFeePerGram * totalEquivalent21K;

        if (totalMfgFee > 0)
        {
            var supplierManufacturingLedgerEntry = SupplierManufacturingLedgerEntry.Create(command.SupplierId, totalMfgFee, currency,
                SupplierBalanceMovementType.Increase, SupplierManufacturingReferenceType.SupplierDelivery, deliveryIds.First(), command.Notes);
            context.SupplierManufacturingLedgerEntries.Add(supplierManufacturingLedgerEntry);

        }
        await context.SaveChangesAsync(cancellationToken);

        return string.Join(",", deliveryIds);
    }
}
