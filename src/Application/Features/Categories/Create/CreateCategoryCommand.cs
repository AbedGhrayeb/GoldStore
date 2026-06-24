using Application.Abstractions.Messaging;

namespace Application.Categories.Create;

public sealed record CreateCategoryCommand(string Name, string? Description, Guid? ParentCategoryId, bool IsActive = true)
    : ICommand<Guid>;