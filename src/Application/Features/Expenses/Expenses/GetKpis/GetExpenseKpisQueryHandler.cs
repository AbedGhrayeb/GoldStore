using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Expenses.Expenses.GetKpis;

internal sealed class GetExpenseKpisQueryHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetExpenseKpisQuery, ExpenseKpiResponse>
{
    private static readonly Dictionary<Currency, (string Code, string Symbol)> CurrencyLabels = new()
    {
        [Currency.Jod] = ("Jod", "د.إ"),
        [Currency.Usd] = ("Usd", "$"),
        [Currency.Ils] = ("Ils", "₪")
    };

    public async Task<Result<ExpenseKpiResponse>> Handle(GetExpenseKpisQuery query, CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.Now);
        DateOnly monthStart = new DateOnly(today.Year, today.Month, 1);

        Dictionary<Guid, Currency> accountCurrencies = await context.FinancialAccounts
            .AsNoTracking()
            .ToDictionaryAsync(a => a.Id, a => a.Currency, cancellationToken);

        var expenses = await context.Expenses
            .AsNoTracking()
            .Where(e => e.ExpenseDate >= monthStart)
            .Select(e => new { e.Id, e.ExpenseDate, e.Amount, e.AccountId, e.ExpenseCategoryId })
            .ToListAsync(cancellationToken);

        var todayExpenses = expenses.Where(e => e.ExpenseDate == today).ToList();
        var monthExpenses = expenses.ToList();

        var todayByCurrency = todayExpenses
            .GroupBy(e => accountCurrencies.GetValueOrDefault(e.AccountId))
            .Where(g => g.Key != default)
            .Select(g => new CurrencyTotal(
                CurrencyLabels.GetValueOrDefault(g.Key).Code ?? g.Key.ToString(),
                CurrencyLabels.GetValueOrDefault(g.Key).Symbol ?? g.Key.ToString(),
                g.Sum(e => e.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var monthByCurrency = monthExpenses
            .GroupBy(e => accountCurrencies.GetValueOrDefault(e.AccountId))
            .Where(g => g.Key != default)
            .Select(g => new CurrencyTotal(
                CurrencyLabels.GetValueOrDefault(g.Key).Code ?? g.Key.ToString(),
                CurrencyLabels.GetValueOrDefault(g.Key).Symbol ?? g.Key.ToString(),
                g.Sum(e => e.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var topCategoryGroup = monthExpenses
            .Where(e => e.ExpenseCategoryId.HasValue)
            .GroupBy(e => new { e.ExpenseCategoryId, Currency = accountCurrencies.GetValueOrDefault(e.AccountId) })
            .Select(g => new { g.Key.ExpenseCategoryId, g.Key.Currency, Total = g.Sum(e => e.Amount) })
            .ToList();

        Guid? topCategoryId = topCategoryGroup
            .GroupBy(x => x.ExpenseCategoryId)
            .Select(g => new { Id = g.Key!.Value, Sum = g.Sum(x => x.Total) })
            .OrderByDescending(g => g.Sum)
            .Select(g => (Guid?)g.Id)
            .FirstOrDefault();

        string topCategoryName = "—";
        List<CurrencyTotal> topCategoryAmounts = [];

        if (topCategoryId.HasValue)
        {
            topCategoryName = await context.ExpenseCategories
                .AsNoTracking()
                .Where(c => c.Id == topCategoryId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? "—";

            topCategoryAmounts = topCategoryGroup
                .Where(x => x.ExpenseCategoryId == topCategoryId && x.Currency != default)
                .Select(x => new CurrencyTotal(
                    CurrencyLabels.GetValueOrDefault(x.Currency).Code ?? x.Currency.ToString(),
                    CurrencyLabels.GetValueOrDefault(x.Currency).Symbol ?? x.Currency.ToString(),
                    x.Total))
                .OrderByDescending(x => x.Amount)
                .ToList();
        }

        return new ExpenseKpiResponse
        {
            TodayTotals = todayByCurrency,
            MonthTotals = monthByCurrency,
            TopCategoryName = topCategoryName,
            TopCategoryAmounts = topCategoryAmounts
        };
    }
}
