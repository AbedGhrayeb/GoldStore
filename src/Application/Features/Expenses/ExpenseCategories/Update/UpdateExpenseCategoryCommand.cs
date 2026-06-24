using Application.Abstractions.Messaging;

namespace Application.Features.Expenses.ExpenseCategories.Update;

public sealed record UpdateExpenseCategoryCommand(Guid Id, string Name) : ICommand<bool>;