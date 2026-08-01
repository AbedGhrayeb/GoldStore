using SharedKernel.Result;

namespace Domain.Suppliers;

public static class SupplierErrors
{
    public static Error NotFound(Guid supplierId) => Error.NotFound(
        "Suppliers.NotFound",
        $"الموردي مع Id = '{supplierId}' غير موجود");

    public static readonly Error NameRequired = Error.Conflict(
        "Suppliers.NameRequired",
        "اسم المورد مطلوب");
    public static readonly Error PrimaryPhoneRequired = Error.Conflict(
        "Suppliers.PrimaryPhoneRequired",
        "رقم الهاتف الأساسي مطلوب");
    public static readonly Error DuplicateName = Error.Conflict(
        "Suppliers.DuplicateName",
        "اسم المورد موجود بالفعل");

    public static readonly Error HasActiveBalance = Error.Conflict(
        "Suppliers.HasActiveBalance",
        "لا يمكن إلغاء تنشيط مورد لديه رصيد معلق من الذهب أو التصنيع");
    public static readonly Error SupplierNotActive = Error.Failure(
        "Suppliers.SupplierNotActive",
        "المورد غير نشط");
}
