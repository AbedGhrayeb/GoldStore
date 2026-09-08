// <copyright file="CreateSupplierScrapGoldPaymentCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Ledger;
using Domain.Common;
using Domain.Inventory;
using Domain.SupplierOperations;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
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

        var karat = (Karat)command.Karat;

        // decimal equivalent21K = GoldWeight.CalculateEquivalent21KWeight(command.WeightInGrams, karat);
        decimal storeStock = await context.GetGoldStockAsync(karat, cancellationToken);

        if (command.WeightInGrams > storeStock)
        {
            return GoldInventoryErrors.InsufficientStock(karat, storeStock, command.WeightInGrams);
        }

        decimal supplierGoldBalance = await context.GetSupplierGoldBalanceAsync(command.SupplierId, karat, cancellationToken);

        if (command.WeightInGrams > supplierGoldBalance)
        {
            return SupplierErrors.InsufficientGoldBalance(karat, supplierGoldBalance, command.WeightInGrams);
        }

        // Decrease the scrap (كسر) category weight. The general gold ledger below already
        // posts one Decrease — the category update here is weight-only on purpose
        // (no InventoryAdjustment posting) to avoid double-counting the store.
        Domain.Catalog.Category? scrapCategory = null;
        if (command.CategoryId.HasValue)
        {
            scrapCategory = await context.Categories
                .FirstOrDefaultAsync(c => c.Id == command.CategoryId.Value, cancellationToken);
            if (scrapCategory is null)
            {
                return Domain.Catalog.CategoryErrors.NotFound(command.CategoryId.Value);
            }
        }
        else
        {
            // Back-compat: no category supplied → fall back to the category named "كسر".
            // Prefer the one matching the payment karat so mixed-karat scrap stays correct.
            List<Domain.Catalog.Category> candidates = await context.Categories
                .Where(c => c.Name == "كسر")
                .ToListAsync(cancellationToken);
            scrapCategory = candidates.FirstOrDefault(c => c.Karat == karat)
                ?? candidates.FirstOrDefault();
        }

        if (scrapCategory is not null)
        {
            if (scrapCategory.Karat.HasValue && scrapCategory.Karat.Value != karat)
            {
                return Error.Validation(
                    "SupplierScrapGold.CategoryKaratMismatch",
                    $"عيار الدفعة ({karat.KaratLabel()}) لا يطابق عيار التصنيف {scrapCategory.Name} ({scrapCategory.Karat.Value.KaratLabel()})");
            }

            Result<Updated> adjusted = scrapCategory.DecreaseWeight(command.WeightInGrams);
            if (adjusted.IsError)
            {
                return adjusted.Errors;
            }
        }

        DateTime paymentDate = dateTimeProvider.UtcNow;
        var payment = SupplierScrapGoldPayment.Create(command.SupplierId, karat, command.WeightInGrams, command.Notes);

        context.SupplierScrapGoldPayments.Add(payment);

        // Record the scrap category in the gold movement notes so the movement
        // shows which catalog category it belongs to alongside the user notes.
        string? movementNotes = WithCategoryNote(command.Notes, scrapCategory?.Name);

        var supplierGoldLedgerEntry = SupplierGoldLedgerEntry.Create(command.SupplierId, karat, command.WeightInGrams,
            SupplierBalanceMovementType.Decrease, SupplierGoldReferenceType.SupplierScrapPayment,
            payment.Id, movementNotes);
        context.SupplierGoldLedgerEntries.Add(supplierGoldLedgerEntry);
        Result<GoldLedgerEntry> goldLedgerEntry = GoldLedgerEntry.Create(karat, command.WeightInGrams,
            GoldMovementType.Decrease, GoldReferenceType.SupplierScrapPayment,
            payment.Id, movementNotes);

        if (goldLedgerEntry.IsError)
        {
            return goldLedgerEntry.Errors;
        }

        context.GoldLedgerEntries.Add(goldLedgerEntry.Value);

        await context.SaveChangesAsync(cancellationToken);

        return payment.Id;
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
