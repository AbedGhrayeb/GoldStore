namespace Domain.Inventory;

public enum GoldMovementType
{
    Increase = 1,
    Decrease = 2
}

public static class GoldMovementTypeExtensions
{
    public static string GetMovementLabel(this GoldMovementType movementType)
    {
        return movementType switch
        {
            GoldMovementType.Increase => "زيادة",
            GoldMovementType.Decrease => "نقصان",
            _ => movementType.ToString()
        };
    }
    public static string GetMovementColor(this GoldMovementType movement) => movement switch
    {
        GoldMovementType.Increase => "text-tertiary-container",
        GoldMovementType.Decrease => "text-error",
        _ => "text-secondary"
    };

    public static string GetMovementIcon(this GoldMovementType movement) => movement switch
    {
        GoldMovementType.Increase => "arrow_downward",
        GoldMovementType.Decrease => "arrow_upward",
        _ => "sync_alt"
    };

    public static string GetReferenceLabel(this GoldReferenceType reference) => reference switch
    {
        GoldReferenceType.SupplierDelivery => "توريد مورد",
        GoldReferenceType.CustomerGoldPurchase => "شراء ذهب عميل",
        GoldReferenceType.Sale => "بيع",
        GoldReferenceType.SupplierScrapPayment => "دفع كسر مورد",
        GoldReferenceType.InventoryAdjustment => "تسوية جردية",
        _ => reference.ToString()
    };
}
