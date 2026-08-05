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
    public static readonly Error TokenGenerationFailed = Error.Failure(
    code: "Auth.TokenGeneration.Failed",
    description: "خط في توليد الرمز");
    public static readonly Error ExpiredAccessTokenInvalid = Error.Conflict(
        code: "Auth.ExpiredAccessToken.Invalid",
        description: "Expired access token is not valid.");
    public static readonly Error RefreshTokenExpired = Error.Conflict(
      code: "Auth.RefreshToken.Expired",
      description: "Refresh token is invalid or has expired.");
    public static Error DatabaseError(Exception ex) => Error.Failure(
     "CustomerPurchaseInvoices.DatabaseError",
     $"حدث خطأ في قاعدة البيانات: {ex.Message}");
}
