namespace Application.Features.Expenses.ExpenseCategories;

public sealed record ExpenseCategoryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}