// <copyright file="HostOnlyAttribute.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace WebUI.Authorization;

/// <summary>
/// Marks an endpoint as host-only. Host administration endpoints authenticate a
/// <c>PlatformUser</c> that carries no tenant, so they are explicitly exempt from
/// tenant resolution instead of relying on path conventions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class HostOnlyAttribute : Attribute;
