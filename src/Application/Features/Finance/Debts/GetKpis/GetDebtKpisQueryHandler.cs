using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Debts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Finance.Debts.GetKpis;

internal sealed class GetDebtKpisQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetDebtKpisQuery, DebtKpiResponse>
{
    private static readonly Dictionary<Currency, (string Code, string Symbol)> CurrencyLabels = new()
    {
        [Currency.Jod] = ("Jod", "د.أ"),
        [Currency.Usd] = ("Usd", "$"),
        [Currency.Ils] = ("Ils", "₪")
    };

    public async Task<Result<DebtKpiResponse>> Handle(GetDebtKpisQuery query, CancellationToken cancellationToken)
    {
        List<Debt> allDebts = await context.Debts.AsNoTracking().ToListAsync(cancellationToken);

        List<Guid> debtIds = allDebts.Select(d => d.Id).ToList();

        List<DebtLedgerEntry> allEntries = await context.DebtLedgerEntries
            .AsNoTracking()
            .Where(e => debtIds.Contains(e.DebtId))
            .ToListAsync(cancellationToken);

        var balances = allEntries
            .GroupBy(e => e.DebtId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(e => e.MovementType == DebtBalanceMovementType.Increase ? e.Amount : -e.Amount));

        decimal totalReceivables = 0m;
        decimal totalPayables = 0m;
        int receivableCount = 0;
        int payableCount = 0;

        var byCurrency = new Dictionary<Currency, (decimal Rec, decimal Pay, int RecCount, int PayCount)>();

        foreach (Debt debt in allDebts)
        {
            decimal balance = balances.GetValueOrDefault(debt.Id, 0m);
            if (balance <= 0) continue;

            if (!byCurrency.TryGetValue(debt.Currency, out var cur))
            {
                cur = (0m, 0m, 0, 0);
            }

            if (debt.Direction == DebtDirection.Receivable)
            {
                totalReceivables += balance;
                receivableCount++;
                cur = (cur.Rec + balance, cur.Pay, cur.RecCount + 1, cur.PayCount);
            }
            else
            {
                totalPayables += balance;
                payableCount++;
                cur = (cur.Rec, cur.Pay + balance, cur.RecCount, cur.PayCount + 1);
            }

            byCurrency[debt.Currency] = cur;
        }

        List<DebtTotalByCurrency> byCurrencyList = byCurrency
            .OrderByDescending(x => x.Value.Rec + x.Value.Pay)
            .Select(x =>
            {
                var (code, symbol) = CurrencyLabels.GetValueOrDefault(x.Key, (x.Key.ToString(), x.Key.ToString()));
                decimal net = x.Value.Rec - x.Value.Pay;
                return new DebtTotalByCurrency
                {
                    Currency = code,
                    Symbol = symbol,
                    TotalReceivables = x.Value.Rec,
                    TotalPayables = x.Value.Pay,
                    NetBalance = net,
                    ReceivableCount = x.Value.RecCount,
                    PayableCount = x.Value.PayCount,
                    TotalReceivablesDisplay = x.Value.Rec.ToString("N3"),
                    TotalPayablesDisplay = x.Value.Pay.ToString("N3"),
                    NetBalanceDisplay = net.ToString("N3")
                };
            })
            .ToList();

        return new DebtKpiResponse
        {
            TotalReceivables = totalReceivables,
            TotalPayables = totalPayables,
            ReceivableCount = receivableCount,
            PayableCount = payableCount,
            NetBalance = totalReceivables - totalPayables,
            TotalReceivablesDisplay = totalReceivables.ToString("N3"),
            TotalPayablesDisplay = totalPayables.ToString("N3"),
            NetBalanceDisplay = (totalReceivables - totalPayables).ToString("N3"),
            IsNetPositive = totalReceivables >= totalPayables,
            ByCurrency = byCurrencyList
        };
    }
}
