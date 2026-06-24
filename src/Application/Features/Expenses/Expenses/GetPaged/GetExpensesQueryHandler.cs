using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Expenses;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Expenses.Expenses.GetPaged;

internal sealed class GetExpensesQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetExpensesQuery, PagedExpenseResponse>
{
    private static string GetCurrencySymbol(string currency) => currency switch
    {
        "Jod" => "د.إ",
        "Usd" => "$",
        "Ils" => "₪",
        _ => currency
    };

    public async Task<Result<PagedExpenseResponse>> Handle(GetExpensesQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Expense> expenses = context.Expenses.AsNoTracking();

        if (query.CategoryId.HasValue)
        {
            expenses = expenses.Where(e => e.ExpenseCategoryId == query.CategoryId);
        }

        if (query.FromDate.HasValue)
        {
            var fromDate = DateOnly.FromDateTime(query.FromDate.Value);
            expenses = expenses.Where(e => e.ExpenseDate >= fromDate);
        }

        if (query.ToDate.HasValue)
        {
            var toDate = DateOnly.FromDateTime(query.ToDate.Value);
            expenses = expenses.Where(e => e.ExpenseDate <= toDate);
        }

        if (!string.IsNullOrWhiteSpace(query.AccountName))
        {
            List<Guid> matchingAccountIds = await context.FinancialAccounts
                .AsNoTracking()
                .Where(a => a.Name.Contains(query.AccountName))
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);

            expenses = expenses.Where(e => matchingAccountIds.Contains(e.AccountId));
        }

        int totalCount = await expenses.CountAsync(cancellationToken);

        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        List<Guid> accountIds = await expenses
            .OrderByDescending(e => e.ExpenseDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => e.AccountId)
            .Distinct()
            .ToListAsync(cancellationToken);

        List<Guid?> categoryIds = await expenses
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => (Guid?)e.ExpenseCategoryId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var categoryGuids = categoryIds.Where(id => id.HasValue).Select(id => id!.Value).ToList();

        Dictionary<Guid, string> accountNames = await context.FinancialAccounts
            .AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);

        Dictionary<Guid, string> accountCurrencies = await context.FinancialAccounts
            .AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Currency.ToString(), cancellationToken);

        Dictionary<Guid, string> categoryNames = await context.ExpenseCategories
            .AsNoTracking()
            .Where(c => categoryGuids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        List<Expense> pagedData = await expenses
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = pagedData.Select(e => new ExpenseResponse
        {
            Id = e.Id,
            ExpenseDate = e.ExpenseDate,
            CategoryId = e.ExpenseCategoryId,
            CategoryName = e.ExpenseCategoryId.HasValue && categoryNames.ContainsKey(e.ExpenseCategoryId.Value)
                ? categoryNames[e.ExpenseCategoryId.Value]
                : "بدون تصنيف",
            Description = e.Description,
            Amount = e.Amount,
            Currency = accountCurrencies.GetValueOrDefault(e.AccountId, string.Empty),
            CurrencySymbol = GetCurrencySymbol(accountCurrencies.GetValueOrDefault(e.AccountId, string.Empty)),
            AccountId = e.AccountId,
            AccountName = accountNames.GetValueOrDefault(e.AccountId, string.Empty)
        }).ToList();

        return new PagedExpenseResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
