using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.CustomerPurchases;
using Domain.Debts;
using Domain.Finance;
using Domain.Inventory;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.CustomerPurchaseInvoices.Create;

internal sealed class CreateCustomerPurchaseInvoiceCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<CreateCustomerPurchaseInvoiceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateCustomerPurchaseInvoiceCommand command, CancellationToken cancellationToken)
    {
        if (command.Items.Count == 0)
        {
            return Result.Failure<Guid>(CustomerPurchaseInvoiceErrors.NoItems);
        }

        if (command.AmountPaid > command.TotalAmount)
        {
            return Result.Failure<Guid>(CustomerPurchaseInvoiceErrors.InvalidPaymentAmount);
        }

        var invoiceId = Guid.CreateVersion7();

        try
        {
            Currency currency = Enum.Parse<Currency>(command.Currency);
            var paymentMethod = (PaymentMethod)command.PaymentMethod;
            FinancialAccountType expectedAccountType = paymentMethod == PaymentMethod.Cash
                ? FinancialAccountType.Cash
                : FinancialAccountType.Bank;

            FinancialAccount? account = await context.FinancialAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == command.AccountId && a.IsActive, cancellationToken);

            if (account is null)
            {
                return Result.Failure<Guid>(CustomerPurchaseInvoiceErrors.AccountNotFound);
            }

            if (account.Currency != currency || account.AccountType != expectedAccountType)
            {
                return Result.Failure<Guid>(CustomerPurchaseInvoiceErrors.AccountDoesNotMatchPayment);
            }
            Guid userId = userContext.UserId;
            string invoiceNumber = await GenerateInvoiceNumberAsync(cancellationToken);
            decimal remainingBalance = command.TotalAmount - command.AmountPaid;

            var invoice = new CustomerPurchaseInvoice
            {
                Id = invoiceId,
                InvoiceNumber = invoiceNumber,
                SellerName = command.SellerName,
                SellerIdNumber = command.SellerIdNumber,
                SellerPhone = command.SellerPhone,
                SeelerYearOfBirth = command.SellerYearOfBirth,
                SellerAddress = command.SellerAddress,
                BuyerName = command.BuyerName,
                Date = command.Date,
                Currency = currency,
                TotalAmount = command.TotalAmount,
                AmountPaid = command.AmountPaid,
                PaymentMethod = paymentMethod,
                AccountId = command.AccountId,
                SellerAccountNumber = command.SellerAccountNumber,
                UserId = userId,
                Notes = command.Notes,
            };

            context.CustomerPurchaseInvoices.Add(invoice);

            foreach (CustomerPurchaseInvoiceItemDto item in command.Items)
            {
                var karat = (Karat)item.Karat;
                decimal equivalent21K = GoldWeight.CalculateEquivalent21KWeight(item.WeightInGrams, karat);
                decimal goldAmount = item.WeightInGrams * item.PricePerGram;

                context.CustomerPurchaseInvoiceItems.Add(new CustomerPurchaseInvoiceItem
                {
                    Id = Guid.CreateVersion7(),
                    CustomerPurchaseInvoiceId = invoiceId,
                    CategoryId = item.CategoryId,
                    Karat = karat,
                    WeightInGrams = item.WeightInGrams,
                    Equivalent21KWeightInGrams = equivalent21K,
                    PricePerGram = item.PricePerGram,
                    GoldAmount = goldAmount
                });
            }

            if (command.AmountPaid > 0)
            {
                context.FinancialTransactions.Add(new FinancialTransaction
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = command.AccountId,
                    Currency = currency,
                    Amount = command.AmountPaid,
                    TransactionType = FinancialTransactionType.Outflow,
                    ReferenceType = FinancialReferenceType.CustomerGoldPurchase,
                    ReferenceId = invoiceId,
                    UserId = userId,
                    Date = command.Date,
                    Notes = $"دفعة فاتورة شراء ذهب {invoiceNumber} — {command.SellerName}"
                });
            }

            if (remainingBalance > 0)
            {
                var debtId = Guid.CreateVersion7();

                context.Debts.Add(new Debt
                {
                    Id = debtId,
                    Name = command.SellerName,
                    Phone = command.SellerPhone,
                    Direction = DebtDirection.Payable,
                    Currency = currency,
                    Notes = $"باقي فاتورة شراء ذهب {invoiceNumber} بتاريخ {command.Date:yyyy-MM-dd}",
                    CreatedAt = command.Date
                });

                context.DebtLedgerEntries.Add(new DebtLedgerEntry
                {
                    Id = Guid.CreateVersion7(),
                    DebtId = debtId,
                    Amount = remainingBalance,
                    MovementType = DebtBalanceMovementType.Increase,
                    Date = command.Date,
                    Notes = $"رصيد متبقي من فاتورة شراء ذهب {invoiceNumber}"
                });
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid>(CustomerPurchaseInvoiceErrors.DatabaseError(ex));
        }

        return Result.Success(invoiceId);
    }

    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken cancellationToken)
    {
        string yearMonth = DateTime.UtcNow.ToString("yyyy-MM");

        int count = await context.CustomerPurchaseInvoices
            .CountAsync(i => i.InvoiceNumber.StartsWith($"PUR-{yearMonth}"), cancellationToken);

        return $"PUR-{yearMonth}-{count + 1:D4}";
    }
}
