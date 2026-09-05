// <copyright file="FeatureRequirement.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Tenants;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Authorization;

/// <summary>
/// Requires the current tenant to have a specific feature enabled. The check reads the
/// scoped <see cref="ICurrentTenant.EnabledFeatures"/>, which is resolved lazily from the
/// database and cached for the request scope (plan Phase 4 item 7).
/// </summary>
internal sealed class FeatureRequirement(string feature) : IAuthorizationRequirement
{
    public string Feature { get; } = feature;
}

internal sealed class FeatureAuthorizationHandler(ICurrentTenant currentTenant)
    : AuthorizationHandler<FeatureRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FeatureRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && currentTenant.IsAvailable
            && currentTenant.EnabledFeatures.Any(feature =>
                feature.Equals(requirement.Feature, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
