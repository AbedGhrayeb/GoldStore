using Application.Abstractions.Messaging;

namespace Application.Features.Expenses.ExpenseCategories.GetAll;

public sealed record GetExpenseCategoriesQuery(bool ActiveOnly = true) : IQuery<List<ExpenseCategoryResponse>>;