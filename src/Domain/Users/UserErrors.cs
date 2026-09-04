using SharedKernel;
using SharedKernel.Result;

namespace Domain.Users;

public static class UserErrors
{
    public static Error IdRequired => Error.Validation(
        "Users.Id.Required",
        $"The user Id is Required");
    public static Error TenantRequired => Error.Validation(
        "Users.Tenant.Required",
        "The tenant Id is required");
    public static Error EmailRequired => Error.Validation(
        "User.Email.Required",
        $"البريد الإلكتروني مطلوب");
    public static Error PasswordRequired => Error.Validation(
        "User.Password.Required",
        $"كلمة المرور مطلوبة");
    public static Error FirstNameRequired => Error.Validation(
        "User.FirstName.Required",
        $"الاسم الأول مطلوب");
    public static Error LastNameRequired => Error.Validation(
        "User.LastName.Required",
        $"الاسم الأخير مطلوب");
    public static Error NotFound(Guid userId) => Error.NotFound(
        "Users.NotFound",
        $"المستخدم بـ Id = '{userId}' غير موجود");

    public static Error Unauthorized() => Error.Forbidden(
        "Users.Unauthorized",
        "المستخدم غير مصرح له بتنفيذ هذه العملية");

    public static readonly Error NotFoundByEmail = Error.NotFound(
        "Users.NotFoundByEmail",
        "المستخدم بريد إلكتروني محدد غير موجود");

    public static readonly Error EmailNotUnique = Error.Conflict(
        "Users.EmailNotUnique",
        "البريد الإلكتروني المقدم غير فريد");

    public static Error RoleRequired => Error.Validation(
        "Users.Role.Required",
        "الدور مطلوب");

    public static readonly Error RefreshTokenRequired = Error.Unauthorized(
        "Users.RefreshToken.Required",
        "رمز التحديث مطلوب");

    public static readonly Error InvalidRefreshTokenExpiry = Error.Unauthorized(
        "Users.RefreshToken.InvalidExpiry",
        "انتهاء صلاحية رمز التحديث غير صالح");
}
