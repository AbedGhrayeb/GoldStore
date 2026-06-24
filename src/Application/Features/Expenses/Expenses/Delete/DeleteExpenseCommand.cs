using Application.Abstractions.Messaging;

namespace Application.Features.Expenses.Expenses.Delete;

public sealed record DeleteExpenseCommand(Guid Id) : ICommand<bool>;