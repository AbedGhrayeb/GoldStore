using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Features.Expenses.Expenses.Delete;

public sealed record DeleteExpenseCommand(Guid Id) : ICommand<Deleted>;
