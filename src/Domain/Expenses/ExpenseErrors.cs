using SharedKernel.Result;

namespace Domain.Expenses;

public static class ExpenseCategoryErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "ExpenseCategories.NotFound",
        $"التصنيف مع Id = '{id}' غير موجود");

    public static readonly Error DuplicateName = Error.Conflict(
        "ExpenseCategories.DuplicateName",
        "اسم التصنيف موجود بالفعل");

    public static readonly Error NameRequired = Error.Validation(
        "ExpenseCategories.NameRequired",
        "اسم تصنيف المصاريف مطلوب");

    public static readonly Error HasExpenses = Error.Conflict(
        "ExpenseCategories.HasExpenses",
        "لا يمكن حذف تصنيف مرتبط بمصروفات");
}

public static class ExpenseErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Expenses.NotFound",
        $"المصاريف مع Id = '{id}' غير موجود");
    public static Error CategoryNotFound(Guid categoryId) => Error.NotFound(
      "ExpenseCategories.NotFound",
      $"التصنيف مع Id = '{categoryId}' غير موجود");
    public static Error AccountNotFound(Guid accountId) => Error.NotFound(
      "ExpenseCategories.NotFound",
     "تصنيف المصروف غير موجود أو غير نشط");
    public static Error ExpenseIdRequired => Error.Validation(
      "ExpenseCategories.ExpenseIdRequired",
      $"معرف المصاريف مطلوب");
    public static Error ExpenseCategoriesRequired => Error.Validation(
      "ExpenseCategories.Required",
      $"التصنيف مطلوب");
    public static Error AccountRequired => Error.Validation(
      "ExpenseCategories.Required",
      $"الحساب مطلوب");
    public static Error AmountMustBePositive => Error.Validation(
      "ExpenseCategories.AmountMustBePositive",
      $"المبلغ يجب أن يكون موجب");
    public static Error AccountInactive => Error.Validation(
      "ExpenseCategories.AccountInactive",
      $"الحساب غير نشط");

}
