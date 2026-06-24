using Domain.Common;
using SharedKernel;

namespace Domain.Debts;

public sealed class Debt : Entity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Phone { get; set; }

    public DebtDirection Direction { get; set; }

    public Currency Currency { get; set; }

    public Guid? AccountId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}
