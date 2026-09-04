using Application.Abstractions.Data;
using Domain.Common;
using Domain.Finance;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Common.Ledger;

public sealed record PaymentLegDto(
    Guid AccountId,
    string Currency,
    decimal Amount,
    decimal ExchangeRate);

public sealed record PaymentLegResult(
    decimal TotalBaseAmount,
    Guid PrimaryAccountId,
    PaymentMethod PrimaryMethod);

public static class PaymentLegProcessor
{
    public static async Task<Result<PaymentLegResult>> ProcessPaymentLegsAsync(
        this IApplicationDbContext context,
        List<PaymentLegDto> legs,
        Currency baseCurrency,
        FinancialTransactionType transactionType,
        FinancialReferenceType referenceType,
        Guid referenceId,
        string notes,
        CancellationToken cancellationToken)
    {
        decimal totalBase = 0m;
        Guid primaryAccountId = Guid.Empty;
        PaymentMethod primaryMethod = PaymentMethod.Cash;

        for (int i = 0; i < legs.Count; i++)
        {
            PaymentLegDto leg = legs[i];

            if (leg.Amount <= 0)
            {
                return PaymentErrors.LegAmountMustBePositive;
            }

            if (!Enum.TryParse<Currency>(leg.Currency, ignoreCase: true, out Currency legCurrency))
            {
                return PaymentErrors.LegAccountCurrencyMismatch(baseCurrency.ToString(), leg.Currency);
            }

            decimal rate = legCurrency == baseCurrency ? 1m : leg.ExchangeRate;

            if (rate <= 0)
            {
                return PaymentErrors.ExchangeRateMustBePositive;
            }

            FinancialAccount? account = await context.FinancialAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == leg.AccountId, cancellationToken);

            if (account is null)
            {
                return PaymentErrors.LegAccountNotFound;
            }

            if (!account.IsActive)
            {
                return PaymentErrors.LegAccountInactive;
            }

            if (account.Currency != legCurrency)
            {
                return PaymentErrors.LegAccountCurrencyMismatch(account.Currency.ToString(), leg.Currency);
            }

            if (transactionType == FinancialTransactionType.Outflow)
            {
                decimal availableBalance = await context.GetAccountBalanceAsync(leg.AccountId, cancellationToken);

                if (leg.Amount > availableBalance)
                {
                    return FinancialAccountErrors.InsufficientBalance(availableBalance, leg.Amount);
                }
            }

            decimal baseAmount = legCurrency == baseCurrency
                ? leg.Amount
                : Math.Round(leg.Amount * rate, 3, MidpointRounding.AwayFromZero);

            Result<FinancialTransaction> financialTransactionResult = FinancialTransaction.Create(
                leg.AccountId, legCurrency, leg.Amount, transactionType, referenceType, referenceId, notes);

            if (financialTransactionResult.IsError)
            {
                return financialTransactionResult.Errors;
            }

            financialTransactionResult.Value.ExchangeRate = rate;
            financialTransactionResult.Value.BaseAmount = baseAmount;
            context.FinancialTransactions.Add(financialTransactionResult.Value);

            totalBase += baseAmount;
            if (i == 0)
            {
                primaryAccountId = leg.AccountId;
                primaryMethod = account.AccountType == FinancialAccountType.Bank
                    ? PaymentMethod.Bank
                    : PaymentMethod.Cash;
            }
        }

        return new PaymentLegResult(totalBase, primaryAccountId, primaryMethod);
    }
}
