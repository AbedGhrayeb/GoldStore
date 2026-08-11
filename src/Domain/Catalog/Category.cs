using SharedKernel;
using SharedKernel.Result;

namespace Domain.Catalog;

public sealed class Category : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }



    public Guid? ParentCategoryId { get;  set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }
    public Category ParentCategory { get; set; }
    public ICollection<Category> Childrens { get; set; } = [];

    public Category()
    {

    }
    public Category(Guid id, Guid? parentCategoryId, string name, string? description) : base(id)
    {
        ParentCategoryId = parentCategoryId;
        Name = name;
        Description = description;
    }
    public static Result<Category> Create(Guid? parentCategoryId, string name, string? description)
    {

        if (string.IsNullOrWhiteSpace(name))
        {
            return CategoryErrors.NameRequired;
        }
        return new Category(Guid.CreateVersion7(), parentCategoryId, name, description);
    }
    public Result<Updated> Update(Guid? parentCategoryId, string name, string? description, bool isActive)
    {

        if (string.IsNullOrWhiteSpace(name))
        {
            return CategoryErrors.NameRequired;
        }
        ParentCategoryId = parentCategoryId;
        Name = name;
        Description = description;
        IsActive = isActive;
        return Result.Updated;
    }
}
