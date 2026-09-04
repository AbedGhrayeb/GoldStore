using SharedKernel.Result;

namespace Domain.Authorization;

public static class AuthorizationErrors
{
    public static Error KeyRequired => Error.Validation(
        "Authorization.Key.Required",
        "المعرف مطلوب");

    public static Error NameRequired => Error.Validation(
        "Authorization.Name.Required",
        "الاسم مطلوب");

    public static Error RoleNotFound(Guid roleId) => Error.NotFound(
        "Authorization.Role.NotFound",
        $"الدور بـ Id = '{roleId}' غير موجود");

    public static Error PermissionNotFound(string key) => Error.NotFound(
        "Authorization.Permission.NotFound",
        $"الصلاحيه '{key}' غير موجودة");
}
