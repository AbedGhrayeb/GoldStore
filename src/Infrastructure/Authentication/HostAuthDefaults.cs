// <copyright file="HostAuthDefaults.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Infrastructure.Authentication;

/// <summary>
/// Dedicated authentication scheme for host administrators (plan Phase 4 item 6).
/// Host identities are <c>PlatformUser</c>s that carry no tenant and can only reach
/// endpoints explicitly marked host-only.
/// </summary>
public static class HostAuthDefaults
{
    public const string AuthenticationScheme = "HostAuth";
}
