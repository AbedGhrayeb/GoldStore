using SharedKernel;

namespace Domain.Suppliers;

public static class SupplierErrors
{
    public static Error NotFound(Guid supplierId) => Error.NotFound(
        "Suppliers.NotFound",
        $"The supplier with Id = '{supplierId}' was not found");

    public static readonly Error DuplicateName = Error.Conflict(
        "Suppliers.DuplicateName",
        "A supplier with the same name already exists");

    public static readonly Error HasActiveBalance = Error.Conflict(
        "Suppliers.HasActiveBalance",
        "Cannot deactivate a supplier that has outstanding gold or manufacturing balance");
}