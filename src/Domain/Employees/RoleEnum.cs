namespace Domain.Employees;

public enum RoleEnum
{
    Admin = 1,
    Manager = 2,
    Accountant = 3,
    Salesperson = 4,
    None = 5
}
public static class RoleEnumExtensions
{
    public static string ToFriendlyString(this RoleEnum role)
    {
        return role switch
        {
            RoleEnum.Admin => "مدير النظام",
            RoleEnum.Manager => "مدير المتجر",
            RoleEnum.Accountant => "محاسب",
            RoleEnum.Salesperson => "موظف مبيعات",
            RoleEnum.None => "غير محدد",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }
}
