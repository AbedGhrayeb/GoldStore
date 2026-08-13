using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Common;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.SupplierFinancialTransactions.GetKpis;

internal sealed class GetSupplierFinancialKpisQueryHandler(
    IApplicationDbContext context,
    ICurrentTenant currentTenant)
    : IQueryHandler<GetSupplierFinancialKpisQuery, SupplierFinancialKpiResponse>
{

    public async Task<Result<SupplierFinancialKpiResponse>> Handle(
        GetSupplierFinancialKpisQuery query, CancellationToken cancellationToken)
    {
        List<SupplierFinancialTransaction> transactions = await context.SupplierFinancialTransactions
            .AsNoTracking()
            .Where(t => t.TenantId == currentTenant.TenantId)
            .ToListAsync(cancellationToken);

        var transactionIds = transactions.Select(t => t.Id).ToList();

        List<SupplierFinancialLedgerEntry> allEntries = await context.SupplierFinancialLedgerEntries
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .Where(e => transactionIds.Contains(e.SupplierFinancialTransactionId))
            .ToListAsync(cancellationToken);

        var balances = allEntries
            .GroupBy(e => e.SupplierFinancialTransactionId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(e => e.MovementType == SupplierBalanceMovementType.Increase ? e.Amount : -e.Amount));

        var byCurrency = new Dictionary<Currency, (decimal From, decimal To, int Count)>();

        foreach (SupplierFinancialTransaction tx in transactions)
        {
            decimal balance = balances.GetValueOrDefault(tx.Id, 0m);
            if (balance <= 0)
            { continue; }

            if (!byCurrency.TryGetValue(tx.Currency, out (decimal From, decimal To, int Count) cur))
            {
                cur = (0m, 0m, 0);
            }

            if (tx.Direction == SupplierFinancialTransactionDirection.FromSupplier)

            { cur = (cur.From + balance, cur.To, cur.Count + 1); }
            else
            { cur = (cur.From, cur.To + balance, cur.Count + 1); }

            byCurrency[tx.Currency] = cur;
        }

        var byCurrencyList = byCurrency
            .OrderByDescending(x => x.Value.From + x.Value.To)
            .Select(x =>
            {
                (string? code, string? symbol) = CurrencyExtensions.CurrencyLabels.GetValueOrDefault(x.Key, (x.Key.ToString(), x.Key.ToString()));
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
