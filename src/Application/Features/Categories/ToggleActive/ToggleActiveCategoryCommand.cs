using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Categories.ToggleActive;

public sealed record ToggleActiveCategoryCommand(Guid Id) : ICommand<Updated>;
