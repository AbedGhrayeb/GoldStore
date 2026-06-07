using Domain.Common;
using SharedKernel;

namespace Domain.Finance;

public sealed class FinancialAccount : Entity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public Currency Currency { get; set; }

    public FinancialAccountType AccountType { get; set; }

    public string? AccountNumber { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
