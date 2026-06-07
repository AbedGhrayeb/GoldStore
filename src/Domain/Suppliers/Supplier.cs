using SharedKernel;

namespace Domain.Suppliers;

public sealed class Supplier : Entity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string PrimaryPhone { get; set; }

    public string? SecondaryPhone { get; set; }

    public string? BankAccountNumber { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
