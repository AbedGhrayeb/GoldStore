using Application.Abstractions.Messaging;

namespace Application.Features.Expenses.Expenses.GetPaged;

public sealed record GetExpensesQuery(
    int Page = 1,
    int PageSize = 20,
    string? AccountName = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    Guid? CategoryId = null) : IQuery<PagedExpenseResponse>;