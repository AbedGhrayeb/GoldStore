// <copyright file="CreateSupplierDeliveryCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Domain.Common;
using Domain.Finance;
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

        Currency currency = Currency.JOD;
        if (!string.IsNullOrWhiteSpace(command.ManufacturingFeeCurrency))
        {
            currency = Enum.Parse<Currency>(command.ManufacturingFeeCurrency, ignoreCase: true);
        }

        if (currency != Currency.JOD)
        {
            return Error.Validation("SupplierDeliveries.ManufacturingFeeCurrency", "عملة أجور التصنيع يجب أن تكون بالدينار (JOD)");
        }

        if (!Enum.TryParse<Currency>(command.AmountDueCurrency, ignoreCase: true, out Currency dueCurrency))
        {
            return Error.Validation("SupplierDeliveries.AmountDueCurrency", "عملة المبلغ المستحق غير صالحة");
        }

        var deliveryIds = new List<Guid>();
        DateTime deliveryDate = DateTime.UtcNow;
        Guid userId = userContext.UserId;

        // Preload the categories touched by this delivery so each category weight is
        // increased exactly once per line weight. The general gold ledger below already
        // posts one Increase per line — the category update here is weight-only on
        // purpose (no InventoryAdjustment posting) to avoid double-counting the store.
        Dictionary<Guid, Domain.Catalog.Category> categoriesById = [];
        List<Guid> requestedCategoryIds = command.Lines
            .Where(l => l.CategoryId.HasValue)
            .Select(l => l.CategoryId!.Value)
            .Distinct()
            .ToList();
        if (requestedCategoryIds.Count > 0)
        {
            categoriesById = await context.Categories
                .Where(c => requestedCategoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, cancellationToken);
            foreach (Guid categoryId in requestedCategoryIds)
            {
                if (!categoriesById.ContainsKey(categoryId))
                {
                    return Domain.Catalog.CategoryErrors.NotFound(categoryId);
                }
            }
        }

        foreach (DeliveryLineDto line in command.Lines)
        {
            var karat = (Karat)line.Karat;
            if (line.CategoryId.HasValue)
            {
                Domain.Catalog.Category category = categoriesById[line.CategoryId.Value];
                if (category.Karat.HasValue && category.Karat.Value != karat)
                {
                    return Error.Validation(
                        "SupplierDeliveries.CategoryKaratMismatch",
                        $"عيار الخط ({karat.KaratLabel()}) لا يطابق عيار التصنيف {category.Name} ({category.Karat.Value.KaratLabel()})");
                }

                Result<Updated> adjusted = category.IncreaseWeight(line.WeightInGrams);
                if (adjusted.IsError)
                {
                    return adjusted.Errors;
                }
            }

            var delivery = SupplierDelivery.Create(
                command.SupplierId,
                karat,
                line.WeightInGrams,
                command.ManufacturingFeePerGram,
                currency,
                command.Notes,
                line.CategoryId);

            context.SupplierDeliveries.Add(delivery);
            deliveryIds.Add(delivery.Id);

            // Record the line category in the gold movement notes so each movement
            // shows which catalog category it belongs to alongside the user notes.
            string? lineNotes = command.Notes;
            if (line.CategoryId.HasValue)
            {
                lineNotes = WithCategoryNote(command.Notes, categoriesById[line.CategoryId.Value].Name);
            }

            // to calc supplier balance
            var supplierGoldLedgerEntry = SupplierGoldLedgerEntry.Create(command.SupplierId, karat, line.WeightInGrams,
                SupplierBalanceMovementType.Increase, SupplierGoldReferenceType.SupplierDelivery, delivery.Id, lineNotes);
            context.SupplierGoldLedgerEntries.Add(supplierGoldLedgerEntry);

            // to calc our store balance
            Result<GoldLedgerEntry> goldLedgerEntry = GoldLedgerEntry.Create(karat, line.WeightInGrams,
                GoldMovementType.Increase, GoldReferenceType.SupplierDelivery, delivery.Id, lineNotes);
            if (goldLedgerEntry.IsError)
            {
                return goldLedgerEntry.Errors;
            }

            context.GoldLedgerEntries.Add(goldLedgerEntry.Value);
        }

        // The header-level due amount is the manufacturing debt for this delivery.
        if (command.AmountDue > 0)
        {
            var supplierManufacturingLedgerEntry = SupplierManufacturingLedgerEntry.Create(command.SupplierId, command.AmountDue, dueCurrency,
                SupplierBalanceMovementType.Increase, SupplierManufacturingReferenceType.SupplierDelivery, deliveryIds.First(), command.Notes);
            context.SupplierManufacturingLedgerEntries.Add(supplierManufacturingLedgerEntry);
        }

        // Optional immediate settlement of the due amount across multiple payment
        // legs and currencies (same pattern as manufacturing payments). Partial
        // payment is allowed; the remainder stays payable via دفع تصنيع.
        if (command.PaymentLegs is { Count: > 0 } legs)
        {
            var paymentId = Guid.CreateVersion7();

            Result<PaymentLegResult> paymentResult = await context.ProcessPaymentLegsAsync(
                legs, dueCurrency, FinancialTransactionType.Outflow, FinancialReferenceType.SupplierManufacturingPayment,
                paymentId, command.Notes ?? "دفعة مستحق تسليم", cancellationToken);

            if (paymentResult.IsError)
            {
                return paymentResult.Errors;
            }

            decimal totalBaseAmount = paymentResult.Value.TotalBaseAmount;
            Guid accountId = paymentResult.Value.PrimaryAccountId;

            decimal mfgBalance = await context.GetSupplierManufacturingBalanceAsync(command.SupplierId, dueCurrency, cancellationToken);

            // NOTE: entries added above are tracked but not yet saved; the balance query
            // reads committed rows only, so include this delivery's fresh debt explicitly.
            decimal pendingDebt = command.AmountDue > 0 ? command.AmountDue : 0M;

            if (totalBaseAmount > mfgBalance + pendingDebt)
            {
                return SupplierErrors.InsufficientManufacturingBalance(dueCurrency, mfgBalance + pendingDebt, totalBaseAmount);
            }

            var payment = SupplierManufacturingPayment.Create(command.SupplierId, accountId, totalBaseAmount, dueCurrency, command.Notes);
            context.SupplierManufacturingPayments.Add(payment);
            var manufacturingPaymentEntry = SupplierManufacturingLedgerEntry.Create(command.SupplierId, totalBaseAmount, dueCurrency,
                SupplierBalanceMovementType.Decrease, SupplierManufacturingReferenceType.SupplierManufacturingPayment, payment.Id, command.Notes);
            context.SupplierManufacturingLedgerEntries.Add(manufacturingPaymentEntry);
        }

        await context.SaveChangesAsync(cancellationToken);

        return string.Join(",", deliveryIds);
    }

    private static string? WithCategoryNote(string? notes, string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return notes;
        }

        string prefix = $"صنف: {categoryName.Trim()}";
        return string.IsNullOrWhiteSpace(notes) ? prefix : $"{prefix} - {notes.Trim()}";
    }
}
