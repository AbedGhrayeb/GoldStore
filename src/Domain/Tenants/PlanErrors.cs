using SharedKernel.Result;

namespace Domain.Tenants;

public static class PlanErrors
{
    public static Error NameRequired => Error.Validation(
        "Plan.Name.Required",
        "اسم الباقة مطلوب");

    public static Error InvalidPrice => Error.Validation(
        "Plan.Price.Invalid",
        "السعر يجب أن يكون صفراً أو أكثر");

    public static Error NotFound(Guid planId) => Error.NotFound(
        "Plan.NotFound",
        $"الباقة بـ Id = '{planId}' غير موجودة");
}
