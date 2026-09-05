// <copyright file="SubscriptionErrors.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel.Result;

namespace Domain.Tenants;

public static class SubscriptionErrors
{
    public static Error TenantRequired => Error.Validation(
        "Subscription.Tenant.Required",
        "المستأجر مطلوب");

    public static Error PlanRequired => Error.Validation(
        "Subscription.Plan.Required",
        "الباقة مطلوبة");

    public static Error InvalidPrice => Error.Validation(
        "Subscription.Price.Invalid",
        "السعر يجب أن يكون صفراً أو أكثر");

    public static Error InvalidDateRange => Error.Validation(
        "Subscription.DateRange.Invalid",
        "تاريخ انتهاء الاشتراك يجب أن يكون بعد تاريخ البدء");

    public static Error NotFound(Guid subscriptionId) => Error.NotFound(
        "Subscription.NotFound",
        $"الاشتراك بـ Id = '{subscriptionId}' غير موجود");
}
