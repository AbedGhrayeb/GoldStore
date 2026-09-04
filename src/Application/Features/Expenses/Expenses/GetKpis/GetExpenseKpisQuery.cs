using Application.Abstractions.Messaging;

namespace Application.Features.Expenses.Expenses.GetKpis;

public sealed record GetExpenseKpisQuery : IQuery<ExpenseKpiResponse>;
