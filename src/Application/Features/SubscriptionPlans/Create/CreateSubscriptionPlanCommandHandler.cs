using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.SubscriptionPlans.Create;

internal sealed class CreateSubscriptionPlanCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CreateSubscriptionPlanCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        string normalizedKey = string.IsNullOrWhiteSpace(command.Key)
            ? SubscriptionPlan.GenerateKey(command.Name)
            : command.Key.Trim().ToLowerInvariant();

        bool exists = await context.SubscriptionPlans
            .AsNoTracking()
            .AnyAsync(plan => plan.Key == normalizedKey, cancellationToken);

        if (exists)
        {
            // Auto-append suffix to guarantee uniqueness when generated
            if (string.IsNullOrWhiteSpace(command.Key))
            {
                normalizedKey = $"{normalizedKey}-{Guid.CreateVersion7():N}"[..12].ToLowerInvariant();
            }
            else
            {
                return TenantErrors.KeyNotUnique;
            }
        }

        Result<SubscriptionPlan> planResult = SubscriptionPlan.Create(
            command.Name,
            normalizedKey,
            command.MaximumActiveUsers,
            command.MaximumPostedInvoicesPerPeriod,
            command.MaximumActiveBranches,
            command.MaximumStorageBytes,
            command.IsTrial,
            command.DurationInMonths,
            command.Price,
            command.DiscountPercent);

        if (planResult.IsError)
        {
            return planResult.Errors;
        }

        context.SubscriptionPlans.Add(planResult.Value);
        await context.SaveChangesAsync(cancellationToken);

        return planResult.Value.Id;
    }
}
