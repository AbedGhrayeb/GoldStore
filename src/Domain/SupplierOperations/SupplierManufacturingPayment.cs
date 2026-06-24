using Domain.Common;
using SharedKernel;

namespace Domain.SupplierOperations;

public sealed class SupplierManufacturingPayment : Entity
{
    public Guid Id { get; set; }

    public Guid SupplierId { get; set; }

    public Guid AccountId { get; set; }

    public decimal Amount { get; set; }

    public Currency Currency { get; set; }

    public DateTime Date { get; set; }

    public string? Notes { get; set; }
}
