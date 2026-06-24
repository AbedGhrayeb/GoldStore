using Application.Abstractions.Messaging;

namespace Application.Features.Expenses.Expenses.Create;

public sealed record CreateExpenseCommand(
    DateOnly ExpenseDate,
    Guid? CategoryId,
    string? Description,
    decimal Amount,
    Guid AccountId) : ICommand<Guid>;