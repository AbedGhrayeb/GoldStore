namespace Domain.Authorization;

/// <summary>
/// The system-wide permission catalog (global reference data, plan Phase 0 item 7).
/// Permissions are granted to global role templates; role templates are assigned to
/// tenant users. The catalog is seeded once and treated as read-only.
/// </summary>
public static class Permissions
{
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";

    public const string EmployeesView = "employees.view";
    public const string EmployeesManage = "employees.manage";

    public const string SuppliersView = "suppliers.view";
    public const string SuppliersManage = "suppliers.manage";

    public const string InventoryView = "inventory.view";
    public const string InventoryManage = "inventory.manage";

    public const string FinanceView = "finance.view";
    public const string FinanceManage = "finance.manage";

    public const string SalesView = "sales.view";
    public const string SalesManage = "sales.manage";

    public const string PurchasesView = "purchases.view";
    public const string PurchasesManage = "purchases.manage";

    public const string ExpensesView = "expenses.view";
    public const string ExpensesManage = "expenses.manage";

    public const string ReportsView = "reports.view";

    public const string SettingsView = "settings.view";
    public const string SettingsManage = "settings.manage";

    /// <summary>Every permission in the catalog, in definition order.</summary>
    public static readonly IReadOnlyList<(string Key, string Name)> All =
    [
        (UsersView, "عرض المستخدمين"),
        (UsersManage, "إدارة المستخدمين"),
        (EmployeesView, "عرض الموظفين"),
        (EmployeesManage, "إدارة الموظفين"),
        (SuppliersView, "عرض الموردين"),
        (SuppliersManage, "إدارة الموردين"),
        (InventoryView, "عرض المخزون"),
        (InventoryManage, "إدارة المخزون"),
        (FinanceView, "عرض المالية"),
        (FinanceManage, "إدارة المالية"),
        (SalesView, "عرض المبيعات"),
        (SalesManage, "إدارة المبيعات"),
        (PurchasesView, "عرض المشتريات"),
        (PurchasesManage, "إدارة المشتريات"),
        (ExpensesView, "عرض المصروفات"),
        (ExpensesManage, "إدارة المصروفات"),
        (ReportsView, "عرض التقارير"),
        (SettingsView, "عرض الإعدادات"),
        (SettingsManage, "إدارة الإعدادات"),
    ];
}
