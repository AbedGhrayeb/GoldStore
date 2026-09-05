// <copyright file="PlatformAdminErrors.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel.Result;

namespace Domain.Tenants;

public static class PlatformAdminErrors
{
    public static Error LoginFailed => Error.Unauthorized(
        "PlatformAdmin.LoginFailed",
        "البريد الإلكتروني أو كلمة المرور غير صحيحة");

    public static Error EmailRequired => Error.Validation(
        "PlatformAdmin.Email.Required",
        "البريد الإلكتروني مطلوب");

    public static Error PasswordRequired => Error.Validation(
        "PlatformAdmin.Password.Required",
        "كلمة المرور مطلوبة");

    public static Error FirstNameRequired => Error.Validation(
        "PlatformAdmin.FirstName.Required",
        "الاسم الأول مطلوب");

    public static Error LastNameRequired => Error.Validation(
        "PlatformAdmin.LastName.Required",
        "الاسم الأخير مطلوب");

    public static Error NotFound(Guid platformAdminId) => Error.NotFound(
        "PlatformAdmin.NotFound",
        $"مسؤول المنصة بـ Id = '{platformAdminId}' غير موجود");
}
