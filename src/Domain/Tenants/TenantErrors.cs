using SharedKernel.Result;

namespace Domain.Tenants;

public static class TenantErrors
{
    public static Error NameRequired => Error.Validation(
        "Tenant.Name.Required",
        "اسم المتجر مطلوب");

    public static Error SubdomainRequired => Error.Validation(
        "Tenant.Subdomain.Required",
        "النطاق الفرعي مطلوب");

    public static Error SubdomainInvalid => Error.Validation(
        "Tenant.Subdomain.Invalid",
        "النطاق الفرعي غير صالح، يجب أن يحتوي على أحرف إنجليزية صغيرة وأرقام وشرطات فقط");

    public static Error SubdomainReserved => Error.Validation(
        "Tenant.Subdomain.Reserved",
        "النطاق الفرعي محجوز ولا يمكن استخدامه");

    public static Error PlanRequired => Error.Validation(
        "Tenant.Plan.Required",
        "الباقة مطلوبة");

    public static Error NotFound(Guid tenantId) => Error.NotFound(
        "Tenant.NotFound",
        $"المستأجر بـ Id = '{tenantId}' غير موجود");
}
