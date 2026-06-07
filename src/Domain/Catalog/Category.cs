using SharedKernel;

namespace Domain.Catalog;

public sealed class Category : Entity
{
    public Guid Id { get; set; }

    public Guid? ParentCategoryId { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
