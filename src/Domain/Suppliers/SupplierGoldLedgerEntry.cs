using Domain.Common;
using SharedKernel;

namespace Domain.Suppliers;

public sealed class SupplierGoldLedgerEntry : Entity
{
    public Guid Id { get; set; }

    public Guid SupplierId { get; set; }

    public Karat Karat { get; set; }

    public decimal WeightInGrams { get; set; }

    public decimal Equivalent21KWeightInGrams { get; set; }

    public SupplierBalanceMovementType MovementType { get; set; }

    public SupplierGoldReferenceType ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public Guid UserId { get; set; }

    public DateTime Date { get; set; }

    public string? Notes { get; set; }
}
