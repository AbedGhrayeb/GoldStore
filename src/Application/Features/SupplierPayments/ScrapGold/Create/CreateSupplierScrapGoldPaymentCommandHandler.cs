using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Inventory;
using Domain.SupplierOperations;
using Domain.Suppliers;
using SharedKernel;
using SharedKernel.Result;

namespace Application.SupplierPayments.ScrapGold.Create;

internal sealed class CreateSupplierScrapGoldPaymentCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateSupplierScrapGoldPaymentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSupplierScrapGoldPaymentCommand command, CancellationToken cancellationToken)
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

        var karat = (Karat)command.Karat;
        // decimal equivalent21K = GoldWeight.CalculateEquivalent21KWeight(command.WeightInGrams, karat);
        DateTime paymentDate = dateTimeProvider.UtcNow;
        var payment = SupplierScrapGoldPayment.Create(command.SupplierId, karat, command.WeightInGrams, command.Notes);


        context.SupplierScrapGoldPayments.Add(payment);
        var supplierGoldLedgerEntry = SupplierGoldLedgerEntry.Create(command.SupplierId, karat, command.WeightInGrams,
            SupplierBalanceMovementType.Decrease, SupplierGoldReferenceType.SupplierScrapPayment,
            payment.Id, command.Notes);
        context.SupplierGoldLedgerEntries.Add(supplierGoldLedgerEntry);
        Result<GoldLedgerEntry> goldLedgerEntry = GoldLedgerEntry.Create(karat, command.WeightInGrams,
            GoldMovementType.Decrease, GoldReferenceType.SupplierScrapPayment,
            payment.Id, command.Notes);

        if (goldLedgerEntry.IsError)
        {
            return goldLedgerEntry.Errors;
        }
        context.GoldLedgerEntries.Add(goldLedgerEntry.Value);


        await context.SaveChangesAsync(cancellationToken);

        return payment.Id;
    }
}
