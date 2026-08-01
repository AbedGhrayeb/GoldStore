using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Categories.Update;

public sealed record UpdateCategoryCommand(Guid Id, string Name, string? Description, Guid? ParentCategoryId, bool IsActive)
    : ICommand<Updated>;
