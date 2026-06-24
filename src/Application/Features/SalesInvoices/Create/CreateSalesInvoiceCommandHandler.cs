using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Debts;
using Domain.Finance;
using Domain.Inventory;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.SalesInvoices.Create;

internal sealed class CreateSalesInvoiceCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<CreateSalesInvoiceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSalesInvoiceCommand command, CancellationToken cancellationToken)
    {
        if (command.Items.Count == 0)
        {
            return Result.Failure<Guid>(SalesInvoiceErrors.NoItems);
        }

        Currency currency = Enum.Parse<Currency>(command.Currency);
        Guid userId = userContext.UserId;

        List<(SalesInvoiceItemDto Dto, decimal Equivalent21K, decimal GoldAmount)> parsedItems = [];

        foreach (SalesInvoiceItemDto item in command.Items)
        {
            var karat = (Karat)item.Karat;
            decimal equivalent21K = GoldWeight.CalculateEquivalent21KWeight(item.WeightInGrams, karat);
            decimal goldAmount = item.WeightInGrams * item.PricePerGram;

            parsedItems.Add((item, equivalent21K, goldAmount));
        }

        decimal remainingBalance = command.TotalAmount - command.AmountPaid;

        if (command.AmountPaid > command.TotalAmount)
        {
            return Result.Failure<Guid>(SalesInvoiceErrors.InvalidPaymentAmount);
        }

        string invoiceNumber = await GenerateInvoiceNumberAsync(cancellationToken);

        var invoiceId = Guid.CreateVersion7();

        SalesInvoiceStatus status = remainingBalance switch
        {
            0 when command.AmountPaid > 0 => SalesInvoiceStatus.Completed,
            > 0 when command.AmountPaid > 0 => SalesInvoiceStatus.PartiallyPaid,
            > 0 when command.AmountPaid == 0 => SalesInvoiceStatus.Draft,
            _ => SalesInvoiceStatus.Draft
        };

        var invoice = new SalesInvoice
        {
            Id = invoiceId,
            InvoiceNumber = invoiceNumber,
            CustomerName = command.CustomerName,
            CustomerPhone = command.CustomerPhone,
            Date = command.Date,
            Currency = currency,
            TotalAmount = command.TotalAmount,
            AmountPaid = command.AmountPaid,
            RemainingBalance = remainingBalance,
            PaymentMethod = command.PaymentMethod.HasValue ? (PaymentMethod)command.PaymentMethod.Value : null,
            AccountId = command.AccountId,
            BuyerAccountNumber = command.BuyerAccountNumber,
            SellerName = command.SellerName,
            Status = status,
            UserId = userId,
            Notes = command.Notes,
            CreatedAt = DateTime.UtcNow
        };

        context.SalesInvoices.Add(invoice);

        foreach ((SalesInvoiceItemDto? dto, decimal equivalent21K, decimal goldAmount) in parsedItems)
        {
            var itemId = Guid.NewGuid();

            context.SalesInvoiceItems.Add(new SalesInvoiceItem
            {
                Id = itemId,
                SalesInvoiceId = invoiceId,
                CategoryId = dto.CategoryId,
                Karat = (Karat)dto.Karat,
                WeightInGrams = dto.WeightInGrams,
                Equivalent21KWeightInGrams = equivalent21K,
                PricePerGram = dto.PricePerGram,
                GoldAmount = goldAmount
            });

            context.GoldLedgerEntries.Add(new GoldLedgerEntry
            {
                Id = Guid.CreateVersion7(),
                Karat = (Karat)dto.Karat,
                WeightInGrams = dto.WeightInGrams,
                Equivalent21KWeightInGrams = equivalent21K,
                MovementType = GoldMovementType.Decrease,
                ReferenceType = GoldReferenceType.Sale,
                ReferenceId = invoiceId,
                UserId = userId,
                Date = command.Date,
                Notes = $"فاتورة مبيعات {invoiceNumber}"
            });
        }

        if (command.AmountPaid > 0 && command.AccountId.HasValue)
        {
            context.FinancialTransactions.Add(new FinancialTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = command.AccountId.Value,
                Currency = currency,
                Amount = command.AmountPaid,
                TransactionType = FinancialTransactionType.Inflow,
                ReferenceType = FinancialReferenceType.SalesPayment,
                ReferenceId = invoiceId,
                UserId = userId,
                Date = command.Date,
                Notes = $"دفعة فاتورة {invoiceNumber} — {command.CustomerName}"
            });
        }

        if (remainingBalance > 0)
        {
            var debtId = Guid.CreateVersion7();

            context.Debts.Add(new Debt
            {
                Id = debtId,
                Name = command.CustomerName,
                Phone = command.CustomerPhone,
                Direction = DebtDirection.Receivable,
                Currency = currency,
                Notes = $"باقي فاتورة {invoiceNumber} بتاريخ {command.Date:yyyy-MM-dd}",
                CreatedAt = command.Date
            });

            context.DebtLedgerEntries.Add(new DebtLedgerEntry
            {
                Id = Guid.CreateVersion7(),
                DebtId = debtId,
                Amount = remainingBalance,
                MovementType = DebtBalanceMovementType.Increase,
                Date = command.Date,
                Notes = $"رصيد متبقي من فاتورة {invoiceNumber}"
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(invoiceId);
    }

    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken cancellationToken)
    {
        string yearMonth = DateTime.UtcNow.ToString("yyyy-MM");

        int count = await context.SalesInvoices
            .CountAsync(i => i.InvoiceNumber.StartsWith($"INV-{yearMonth}"), cancellationToken);

        return $"INV-{yearMonth}-{count + 1:D4}";
    }
}
