using SharedKernel;

namespace Domain.Debts;

public sealed class DebtLedgerEntry : Entity
{
    public Guid Id { get; set; }

    public Guid DebtId { get; set; }

    public decimal Amount { get; set; }

    public DebtBalanceMovementType MovementType { get; set; }

    public DateTime Date { get; set; }

    public string? Notes { get; set; }
}
