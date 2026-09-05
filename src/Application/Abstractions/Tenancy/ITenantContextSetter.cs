// <copyright file="ITenantContextSetter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Abstractions.Tenancy;

/// <summary>
/// Write side of the tenant context. Used by the resolution middleware (per request)
/// and by background services (per scope) to establish the current tenant.
/// </summary>
public interface ITenantContextSetter
{
    void Set(TenantInfo tenant, bool isReadOnly);
}
