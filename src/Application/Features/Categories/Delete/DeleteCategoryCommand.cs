using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Categories.Delete;

public sealed record DeleteCategoryCommand(Guid Id) : ICommand<Deleted>;
