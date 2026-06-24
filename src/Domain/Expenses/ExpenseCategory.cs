using SharedKernel;

namespace Domain.Expenses;

public sealed class ExpenseCategory : Entity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}