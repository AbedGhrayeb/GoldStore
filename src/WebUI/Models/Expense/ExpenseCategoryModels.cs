namespace WebUI.Models.Expense;

public class ExpenseCategoryModel
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateExpenseCategoryModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}