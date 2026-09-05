// <copyright file="TenantMigrationLogErrors.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel.Result;

namespace Domain.Tenants;

public static class TenantMigrationLogErrors
{
    public static readonly Error TenantRequired = Error.Validation("TenantMigrationLog.TenantRequired", "معرف المستأجر مطلوب.");

    public static readonly Error MigrationIdRequired = Error.Validation("TenantMigrationLog.MigrationIdRequired", "معرف الترحيل مطلوب.");
}
