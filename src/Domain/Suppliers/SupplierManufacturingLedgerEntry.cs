using Domain.Common;
using SharedKernel;

namespace Domain.Suppliers;

public sealed class SupplierManufacturingLedgerEntry : Entity
{
    public Guid Id { get; set; }

    public Guid SupplierId { get; set; }

    public decimal Amount { get; set; }

    public Currency Currency { get; set; }

    public SupplierBalanceMovementType MovementType { get; set; }

    public SupplierManufacturingReferenceType ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public Guid UserId { get; set; }

    public DateTime Date { get; set; }

    public string? Notes { get; set; }
}
