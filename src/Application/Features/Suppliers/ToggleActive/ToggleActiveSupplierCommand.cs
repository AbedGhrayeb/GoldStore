using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Suppliers.ToggleActive;

public sealed record ToggleActiveSupplierCommand(Guid Id) : ICommand<Updated>;
