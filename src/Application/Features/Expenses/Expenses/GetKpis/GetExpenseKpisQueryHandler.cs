using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Result;

namespace Application.Features.Expenses.Expenses.GetKpis;

internal sealed class GetExpenseKpisQueryHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider, ICurrentTenant currentTenant)
    : IQueryHandler<GetExpenseKpisQuery, ExpenseKpiResponse>
{
    public async Task<Result<ExpenseKpiResponse>> Handle(GetExpenseKpisQuery query, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.Now);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        Dictionary<Guid, Currency> accountCurrencies = await context.FinancialAccounts
            .AsNoTracking()
            .Where(a => a.TenantId == currentTenant.TenantId)
            .ToDictionaryAsync(a => a.Id, a => a.Currency, cancellationToken);

        var expenses = await context.Expenses
            .AsNoTracking()
            .Where(e => e.TenantId == currentTenant.TenantId)
            .Where(e => e.ExpenseDate >= monthStart && e.AccountId.HasValue && e.ExpenseCategoryId.HasValue)
            .Select(e => new { e.Id, e.ExpenseDate, e.Amount, e.AccountId, e.ExpenseCategoryId })
            .ToListAsync(cancellationToken);

        var todayExpenses = expenses.Where(e => e.ExpenseDate == today).ToList();
        var monthExpenses = expenses.ToList();

        var todayByCurrency = todayExpenses
            .GroupBy(e => accountCurrencies.GetValueOrDefault(e.AccountId!.Value))
            .Where(g => g.Key != default)
            .Select(g => new CurrencyTotal(
                CurrencyExtensions.CurrencyLabels.GetValueOrDefault(g.Key).Code ?? g.Key.ToString(),
                CurrencyExtensions.CurrencyLabels.GetValueOrDefault(g.Key).Symbol ?? g.Key.ToString(),
                g.Sum(e => e.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var monthByCurrency = monthExpenses
            .GroupBy(e => accountCurrencies.GetValueOrDefault(e.AccountId!.Value))
            .Where(g => g.Key != default)
            .Select(g => new CurrencyTotal(
                CurrencyExtensions.CurrencyLabels.GetValueOrDefault(g.Key).Code ?? g.Key.ToString(),
                CurrencyExtensions.CurrencyLabels.GetValueOrDefault(g.Key).Symbol ?? g.Key.ToString(),
                g.Sum(e => e.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var topCategoryGroup = monthExpenses
            .Where(e => e.ExpenseCategoryId.HasValue)
            .GroupBy(e => new { e.ExpenseCategoryId, Currency = accountCurrencies.GetValueOrDefault(e.AccountId!.Value) })
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
                .Where(c => c.TenantId == currentTenant.TenantId)
                .Where(c => c.Id == topCategoryId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? "—";

            topCategoryAmounts = topCategoryGroup
                .Where(x => x.ExpenseCategoryId == topCategoryId && x.Currency != default)
                .Select(x => new CurrencyTotal(
                    CurrencyExtensions.CurrencyLabels.GetValueOrDefault(x.Currency).Code ?? x.Currency.ToString(),
                    CurrencyExtensions.CurrencyLabels.GetValueOrDefault(x.Currency).Symbol ?? x.Currency.ToString(),
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
