using Domain.Common;
using SharedKernel;

namespace Domain.Finance;

public sealed class FinancialTransaction : Entity
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public Currency Currency { get; set; }

    public decimal Amount { get; set; }

    public FinancialTransactionType TransactionType { get; set; }

    public FinancialReferenceType ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public Guid UserId { get; set; }

    public DateTime Date { get; set; }

    public string? Notes { get; set; }
}
