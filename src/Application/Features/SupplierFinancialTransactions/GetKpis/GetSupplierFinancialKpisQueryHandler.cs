using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.SupplierFinancialTransactions.GetKpis;

internal sealed class GetSupplierFinancialKpisQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetSupplierFinancialKpisQuery, SupplierFinancialKpiResponse>
{
    private static readonly Dictionary<Currency, (string Code, string Symbol)> CurrencyLabels = new()
    {
        [Currency.Jod] = ("Jod", "د.أ"),
        [Currency.Usd] = ("Usd", "$"),
        [Currency.Ils] = ("Ils", "₪")
    };

    public async Task<Result<SupplierFinancialKpiResponse>> Handle(
        GetSupplierFinancialKpisQuery query, CancellationToken cancellationToken)
    {
        var transactions = await context.SupplierFinancialTransactions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var transactionIds = transactions.Select(t => t.Id).ToList();

        var allEntries = await context.SupplierFinancialLedgerEntries
            .AsNoTracking()
            .Where(e => transactionIds.Contains(e.SupplierFinancialTransactionId))
            .ToListAsync(cancellationToken);

        var balances = allEntries
            .GroupBy(e => e.SupplierFinancialTransactionId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Amount : -e.Amount));

        var byCurrency = new Dictionary<Currency, (decimal From, decimal To, int Count)>();

        foreach (var tx in transactions)
        {
            decimal balance = balances.GetValueOrDefault(tx.Id, 0m);
            if (balance <= 0) continue;

            if (!byCurrency.TryGetValue(tx.Currency, out var cur))
                cur = (0m, 0m, 0);

            if (tx.Direction == SupplierFinancialTransactionDirection.FromSupplier)
                cur = (cur.From + balance, cur.To, cur.Count + 1);
            else
                cur = (cur.From, cur.To + balance, cur.Count + 1);

            byCurrency[tx.Currency] = cur;
        }

        var byCurrencyList = byCurrency
            .OrderByDescending(x => x.Value.From + x.Value.To)
            .Select(x =>
            {
                var (code, symbol) = CurrencyLabels.GetValueOrDefault(x.Key, (x.Key.ToString(), x.Key.ToString()));
                decimal net = x.Value.From - x.Value.To;
                return new SupplierFinancialKpiByCurrency
                {
                    Currency = code,
                    Symbol = symbol,
                    TotalFromSupplier = x.Value.From,
                    TotalToSupplier = x.Value.To,
                    NetBalance = net,
                    TransactionCount = x.Value.Count,
                    TotalFromSupplierDisplay = x.Value.From.ToString("N3"),
                    TotalToSupplierDisplay = x.Value.To.ToString("N3"),
                    NetBalanceDisplay = net.ToString("N3")
                };
            })
            .ToList();

        return new SupplierFinancialKpiResponse
        {
            ByCurrency = byCurrencyList
        };
    }
}
