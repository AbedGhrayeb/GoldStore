using Application.Abstractions.Messaging;

namespace Application.Features.Expenses.ExpenseCategories.Create;

public sealed record CreateExpenseCategoryCommand(string Name) : ICommand<Guid>;
