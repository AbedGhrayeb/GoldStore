using Application.Abstractions.Messaging;

namespace Application.Features.Expenses.Expenses.Update;

public sealed record UpdateExpenseCommand(
    Guid Id,
    DateOnly ExpenseDate,
    Guid? CategoryId,
    string? Description,
    decimal Amount,
    Guid AccountId) : ICommand<bool>;