using SharedKernel.Result;

namespace Domain.Tenants;

public static class TenantErrors
{
    public static Error IdRequired => Error.Validation("Tenants.Id.Required", "Tenant Id is required.");

    public static Error NameRequired => Error.Validation("Tenants.Name.Required", "Tenant name is required.");

    public static Error DisplayNameRequired => Error.Validation("Tenants.DisplayName.Required", "Store display name is required.");

    public static Error TimeZoneRequired => Error.Validation("Tenants.TimeZone.Required", "Time zone is required.");

    public static Error KeyRequired => Error.Validation("Tenants.Key.Required", "Tenant key is required.");

    public static Error KeyInvalid => Error.Validation("Tenants.Key.Invalid", "Tenant key must contain only lowercase letters, numbers, and hyphens.");

    public static Error InvalidSubscriptionPeriod => Error.Validation("Tenants.SubscriptionPeriod.Invalid", "Subscription end date must be after its start date.");

    public static Error InvalidPlanLimit => Error.Validation("Tenants.PlanLimit.Invalid", "Subscription plan limits cannot be negative.");

    public static Error EmailRequired => Error.Validation("PlatformUsers.Email.Required", "Platform user email is required.");

    public static Error PasswordRequired => Error.Validation("PlatformUsers.Password.Required", "Platform user password is required.");

    public static Error QuotaExceeded(string resource, int limit) => Error.Conflict(
        "Tenants.Quota.Exceeded",
        $"تم الوصول إلى الحد الأقصى المسموح لعدد {resource} ({limit}). يرجى التواصل مع الدعم لترقية الاشتراك.");

    public static Error NotFound(Guid tenantId) => Error.NotFound(
        "Tenants.NotFound",
        $"المتجر بـ Id = '{tenantId}' غير موجود");

    public static readonly Error KeyNotUnique = Error.Conflict(
        "Tenants.Key.NotUnique",
        "المعرف الخاص بالمتجر مستخدم بالفعل من متجر آخر");

    public static Error PlanNotFound(Guid planId) => Error.NotFound(
        "Tenants.Plan.NotFound",
        $"خطة الاشتراك بـ Id = '{planId}' غير موجودة");

    public static readonly Error StoreAdministratorRoleNotFound = Error.Failure(
        "Tenants.StoreAdminRole.NotFound",
        "دور مدير المتجر غير موجود. تأكد من تهيئة البيانات الأساسية قبل إنشاء المتجر.");

    public static readonly Error AdminEmailRequired = Error.Validation(
        "Tenants.AdminEmail.Required",
        "البريد الإلكتروني لمسؤول المتجر مطلوب");

    public static readonly Error AdminPasswordRequired = Error.Validation(
        "Tenants.AdminPassword.Required",
        "كلمة مرور مسؤول المتجر مطلوبة");

    public static Error InvalidTransition(TenantStatus from, TenantStatus to) => Error.Validation(
        "Tenants.Status.InvalidTransition",
        $"لا يمكن تحويل المتجر من الحالة {from} إلى الحالة {to}");

    public static Error TransitionDateRequired(TenantStatus target) => Error.Validation(
        "Tenants.Status.TransitionDate.Required",
        $"يجب تحديد تاريخ الانتقال لحالة {target}");

    public static Error InvalidPlanDuration => Error.Validation(
        "Tenants.Plan.Duration.Invalid",
        "مدة الخطة يجب أن تكون بين 1 و 60 شهراً.");

    public static Error InvalidTrialDuration => Error.Validation(
        "Tenants.Plan.TrialDuration.Invalid",
        "الخطة التجريبية يجب أن تكون مدتها شهر واحد فقط.");

    public static Error InvalidPlanPrice => Error.Validation(
        "Tenants.Plan.Price.Invalid",
        "سعر الخطة يجب أن يكون صفر أو أكثر.");

    public static Error InvalidDiscountPercent => Error.Validation(
        "Tenants.Plan.Discount.Invalid",
        "نسبة الخصم يجب أن تكون بين 0 و 100.");

    public static Error TrialCannotRenew => Error.Validation(
        "Tenants.Plan.TrialCannotRenew",
        "خطة التجربة لا يمكن تجديدها، يجب الترقية إلى خطة مدفوعة.");
}
