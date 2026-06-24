namespace Application.Categories;

public sealed record CategoryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? ParentCategoryId { get; init; }
    public string? ParentCategoryName { get; init; }
    public bool IsActive { get; init; }
}
