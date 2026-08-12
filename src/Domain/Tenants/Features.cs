namespace Domain.Tenants;

/// <summary>
/// The system-wide feature keys a tenant can enable (plan Phase 4 item 7). Features are
/// stored on <see cref="TenantSettings.EnabledFeatures"/> and enforced by the
/// <c>RequireFeature</c> authorization gate.
/// </summary>
public static class Features
{
    public const string Catalog = "catalog";
    public const string Suppliers = "suppliers";
    public const string Inventory = "inventory";
    public const string Finance = "finance";
    public const string Sales = "sales";
    public const string Purchases = "purchases";
    public const string Hr = "hr";
    public const string Expenses = "expenses";
    public const string Reports = "reports";
    public const string Settings = "settings";

    /// <summary>The full feature catalog, in definition order.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        Catalog,
        Suppliers,
        Inventory,
        Finance,
        Sales,
        Purchases,
        Hr,
        Expenses,
        Reports,
        Settings,
    ];
}
