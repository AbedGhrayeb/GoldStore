using Domain.Finance;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Expenses;

public sealed class Expense : AuditableEntity
{

    public Guid? ExpenseCategoryId { get; private set; }

    public Guid? AccountId { get; private set; }

    public decimal Amount { get; private set; }

    public string? Description { get; private set; }

    public DateOnly ExpenseDate { get; private set; }

    // Navigation properties
    public ExpenseCategory? ExpenseCategory { get; set; } = null;
    public FinancialAccount? FinancialAccount { get; set; }

    private Expense()
    {

    }
    private Expense(Guid id, Guid? expenseCategoryId, Guid? accountId, decimal amount, string? description, DateOnly expenseDate) : base(id)
    {
        ExpenseCategoryId = expenseCategoryId;
        AccountId = accountId;
        Amount = amount;
        Description = description;
        ExpenseDate = expenseDate;
    }

    public static Result<Expense> Create(Guid? expenseCategoryId, Guid? accountId, decimal amount, string? description, DateOnly expenseDate)
    {
        if (!expenseCategoryId.HasValue || expenseCategoryId == Guid.Empty)
        {
            return ExpenseErrors.ExpenseCategoriesRequired;
        }
        if (!accountId.HasValue || accountId == Guid.Empty)
        {
            return ExpenseErrors.AccountRequired;
        }
        if (amount <= 0)
        {
            return ExpenseErrors.AmountMustBePositive;
        }
        return new Expense(Guid.CreateVersion7(), expenseCategoryId, accountId, amount, description, expenseDate);
    }
    public Result<Updated> Update
        (Guid id, Guid? expenseCategoryId, Guid? accountId, decimal amount, string? description, DateOnly expenseDate)

    {
        if (id == Guid.Empty)
        {
            return ExpenseErrors.ExpenseIdRequired;
        }
        if (!expenseCategoryId.HasValue || expenseCategoryId == Guid.Empty)
        {
            return ExpenseErrors.ExpenseCategoriesRequired;
        }
        if (!accountId.HasValue || accountId == Guid.Empty)
        {
            return ExpenseErrors.AccountRequired;
        }
        if (amount <= 0)
        {
            return ExpenseErrors.AmountMustBePositive;
        }

        ExpenseCategoryId = expenseCategoryId!.Value;
        AccountId = accountId!.Value;
        Amount = amount;
        Description = description;
        ExpenseDate = expenseDate;

        return Result.Updated;
    }
}
