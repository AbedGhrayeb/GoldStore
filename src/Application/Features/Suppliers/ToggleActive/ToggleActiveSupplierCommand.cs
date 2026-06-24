using Application.Abstractions.Messaging;

namespace Application.Suppliers.ToggleActive;

public sealed record ToggleActiveSupplierCommand(Guid Id) : ICommand<bool>;