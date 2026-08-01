using SharedKernel;
using SharedKernel.Result;

namespace Domain.Expenses;

public sealed class ExpenseCategory : AuditableEntity
{
    public string Name { get; private set; }
    private readonly List<Expense> _expenses = new();
    public IReadOnlyCollection<Expense> Expenses => _expenses.AsReadOnly();

    private ExpenseCategory()
    {

    }
    private ExpenseCategory(string name)
    {
        Name = name;
    }

    public static Result<ExpenseCategory> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ExpenseCategoryErrors.NameRequired;
        }
        return new ExpenseCategory(name);
    }
    public Result<Updated> Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ExpenseCategoryErrors.NameRequired;
        }
        Name = name;
        return Result.Updated;
    }
}
