using SharedKernel.Result;

namespace Application.Common.Errors;

public static class ApplicationErrors
{
    public static Error EmptyUsersList =>
    Error.Failure(
           "ApplicationErrors.Users.NotFoundAnyUsers",
           "المستخدم غير موجود.");
    public static Error LoginFailed =>
    Error.Failure(
           "ApplicationErrors.Users.LoginFailed",
           $"البريد الإلكتروني أو كلمة المرور غير صحيحة.");
    public static Error TenantAccessDenied =>
    Error.Failure(
           "ApplicationErrors.Tenants.AccessDenied",
           "هذا الحساب غير مفعّل حالياً. يرجى التواصل مع الدعم.");
    public static Error UserDisabled =>
    Error.Failure(
           "ApplicationErrors.Users.Disabled",
           "هذا الحساب معطّل حالياً.");
    public static Error InvalidRefreshToken =>
    Error.Unauthorized(
           "ApplicationErrors.Users.InvalidRefreshToken",
           "رمز التحديث غير صالح أو منتهي الصلاحية.");
    public static Error DatabaseError(Exception ex) => Error.Failure(
     "CustomerPurchaseInvoices.DatabaseError",
     $"حدث خطأ في قاعدة البيانات: {ex.Message}");
}
