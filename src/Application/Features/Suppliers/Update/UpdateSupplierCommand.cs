using Application.Abstractions.Messaging;

namespace Application.Suppliers.Update;

public sealed record UpdateSupplierCommand(
    Guid Id,
    string Name,
    string PrimaryPhone,
    string? SecondaryPhone,
    string? BankAccountNumber,
    string? Notes,
    bool IsActive)
    : ICommand<bool>;