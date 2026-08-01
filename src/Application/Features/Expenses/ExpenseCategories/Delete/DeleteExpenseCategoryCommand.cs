using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Features.Expenses.ExpenseCategories.Delete;

public sealed record DeleteExpenseCategoryCommand(Guid Id) : ICommand<Deleted>;
