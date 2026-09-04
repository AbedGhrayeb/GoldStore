using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Authorization;

/// <summary>
/// Marks an endpoint as requiring an enabled tenant feature (plan Phase 4 item 7).
/// The feature key maps to a policy named <c>feature:&lt;key&gt;</c> handled by
/// <see cref="FeatureRequirement"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireFeatureAttribute(string feature) : AuthorizeAttribute($"feature:{feature}");
