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
    public static Error AccountLocked =>
    Error.Failure(
           "ApplicationErrors.Users.AccountLocked",
           "تم قفل الحساب مؤقتاً بسبب محاولات تسجيل دخول فاشلة متكررة. حاول مرة أخرى لاحقاً.");
    public static Error InvalidRefreshToken =>
    Error.Unauthorized(
           "ApplicationErrors.Users.InvalidRefreshToken",
           "رمز التحديث غير صالح أو منتهي الصلاحية.");
    public static readonly Error DatabaseError = Error.Failure(
        "Application.DatabaseError",
        "حدث خطأ غير متوقع أثناء حفظ البيانات");
}
