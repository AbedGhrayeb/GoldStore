using Application.Abstractions.Messaging;

namespace Application.Categories.ToggleActive;

public sealed record ToggleActiveCategoryCommand(Guid Id) : ICommand<bool>;