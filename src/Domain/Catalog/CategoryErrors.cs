using SharedKernel;

namespace Domain.Catalog;

public static class CategoryErrors
{
    public static Error NotFound(Guid categoryId) => Error.NotFound(
        "Categories.NotFound",
        $"The category with Id = '{categoryId}' was not found");

    public static readonly Error DuplicateName = Error.Conflict(
        "Categories.DuplicateName",
        "A category with the same name already exists at this level");

    public static readonly Error HasChildren = Error.Conflict(
        "Categories.HasChildren",
        "Cannot deactivate a category that has active sub-categories");
}