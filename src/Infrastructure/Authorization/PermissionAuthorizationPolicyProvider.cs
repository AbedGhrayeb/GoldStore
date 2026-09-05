// <copyright file="PermissionAuthorizationPolicyProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Infrastructure.Authorization;

internal sealed class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private const string FeaturePolicyPrefix = "feature:";

    private readonly AuthorizationOptions authorizationOptions;

    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
        this.authorizationOptions = options.Value;
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        AuthorizationPolicy? policy = await base.GetPolicyAsync(policyName);

        if (policy is not null)
        {
            return policy;
        }

        AuthorizationPolicy permissionPolicy = policyName.StartsWith(FeaturePolicyPrefix, StringComparison.Ordinal)
            ? new AuthorizationPolicyBuilder()
                .AddRequirements(new FeatureRequirement(policyName[FeaturePolicyPrefix.Length..]))
                .Build()
            : new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();

        this.authorizationOptions.AddPolicy(policyName, permissionPolicy);

        return permissionPolicy;
    }
}
