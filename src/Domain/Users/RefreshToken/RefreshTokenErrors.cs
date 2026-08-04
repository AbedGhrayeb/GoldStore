using SharedKernel.Result;

namespace Domain.Users.RefreshToken;

public static class RefreshTokenErrors
{
    public static readonly Error IdRequired =
        Error.Validation("RefreshToken_Id_Required", "معرف الرمز مطلوب");

    public static readonly Error TokenRequired =
        Error.Validation("RefreshToken_Token_Required", "قيمة الرمز مطلوبة");

    public static readonly Error UserIdRequired =
        Error.Validation("RefreshToken_UserId_Required", "معرف المستخدم مطلوب");

    public static readonly Error ExpiryInvalid =
        Error.Validation("RefreshToken_Expiry_Invalid", "يجب أن يكون تاريخ انتهاء في المستقبل");
}
