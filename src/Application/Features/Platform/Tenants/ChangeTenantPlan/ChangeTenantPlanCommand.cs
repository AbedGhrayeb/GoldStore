using Application.Abstractions.Messaging;
using Domain.Tenants;
using SharedKernel.Result;

namespace Application.Features.Platform.Tenants.ChangeTenantPlan;

public sealed record ChangeTenantPlanCommand(Guid TenantId, Guid NewPlanId, BillingInterval Interval) : ICommand<Updated>;
