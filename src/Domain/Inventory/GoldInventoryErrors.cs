using Domain.Common;
using SharedKernel.Result;

namespace Domain.Inventory;

public static class GoldInventoryErrors
{
    public static Error InsufficientStock(Karat karat, decimal available, decimal required) => Error.Conflict(
        "Inventory.InsufficientGoldStock",
        $"رصيد الذهب غير كافٍ في {karat.KaratLabel()} (المتاح: {available:N2} جم، المطلوب: {required:N2} جم)");
}
