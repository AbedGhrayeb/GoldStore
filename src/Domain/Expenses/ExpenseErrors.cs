using SharedKernel;

namespace Domain.Expenses;

public static class ExpenseCategoryErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "ExpenseCategories.NotFound",
        $"The expense category with Id = '{id}' was not found");

    public static readonly Error DuplicateName = Error.Conflict(
        "ExpenseCategories.DuplicateName",
        "A category with the same name already exists");

    public static readonly Error HasExpenses = Error.Conflict(
        "ExpenseCategories.HasExpenses",
        "لا يمكن حذف تصنيف مرتبط بمصروفات");
}

public static class ExpenseErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Expenses.NotFound",
        $"The expense with Id = '{id}' was not found");
}