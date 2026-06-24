using Application.Abstractions.Messaging;

namespace Application.Features.Expenses.ExpenseCategories.Delete;

public sealed record DeleteExpenseCategoryCommand(Guid Id) : ICommand<bool>;