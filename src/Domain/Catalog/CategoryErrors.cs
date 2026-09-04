using SharedKernel.Result;

namespace Domain.Catalog;

public static class CategoryErrors
{
    public static Error IdRequired => Error.Validation(
        "Categories.IdRequired",
        $"The category Id is required");
    public static Error NameRequired => Error.Validation(
        "Categories.NameRequired",
        $"اسم الفئة مطلوب");
    public static Error NotFound(Guid categoryId) => Error.NotFound(
        "Categories.NotFound",
        $"الفئة بـ Id = '{categoryId}' غير موجودة");

    public static readonly Error DuplicateName = Error.Conflict(
        "Categories.DuplicateName",
        "اسم الفئة موجود بالفعل على هذا المستوى");

    public static readonly Error HasChildren = Error.Conflict(
        "Categories.HasChildren",
        "لا يمكن تعطيل فئة تحتوي على فئات فرعية نشطة");

    public static readonly Error CircularReference = Error.Conflict(
        "Categories.CircularReference",
        "لا يمكن أن تكون الفئة والمصدر نفسه.");

    public static readonly Error HasReferences = Error.Conflict(
        "Categories.HasReferences",
        "لا يمكن حذف فئة مستخدمة في فواتير البيع أو الشراء");
}
