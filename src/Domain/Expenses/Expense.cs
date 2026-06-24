using Domain.Common;
using SharedKernel;

namespace Domain.Expenses;

public sealed class Expense : Entity
{
    public Guid Id { get; set; }

    public Guid? ExpenseCategoryId { get; set; }

    public Guid AccountId { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateOnly ExpenseDate { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
}