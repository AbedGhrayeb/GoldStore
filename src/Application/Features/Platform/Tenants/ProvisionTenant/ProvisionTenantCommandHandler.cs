using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenancy;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;
using SharedKernel.Result;

namespace Application.Features.Platform.Tenants.ProvisionTenant;

internal sealed class ProvisionTenantCommandHandler(
    IPlatformDbContext platform,
    ITenantSchemaProvisioner schemaProvisioner,
    ITenantSeeder tenantSeeder,
    IDateTimeProvider dateTimeProvider,
    ILogger<ProvisionTenantCommandHandler> logger)
    : ICommandHandler<ProvisionTenantCommand, Guid>
{
    private const int TrialDays = 14;

    public async Task<Result<Guid>> Handle(ProvisionTenantCommand command, CancellationToken cancellationToken)
    {
        string subdomain = command.Subdomain.Trim().ToLowerInvariant();

        Tenant? tenant = await platform.Tenants
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain, cancellationToken);

        if (tenant is null)
        {
            Result<Tenant> createResult = await CreateCatalogEntry(command, subdomain, cancellationToken);
            if (createResult.IsError)
            {
                return createResult.Errors;
            }

            tenant = createResult.Value;
        }
        else
        {
            // Resume after a previous partial failure — the steps below are idempotent.
            logger.LogInformation("Tenant {Subdomain} already exists, resuming provisioning", subdomain);
        }

        await schemaProvisioner.ProvisionAsync(tenant.SchemaName, tenant.ConnectionString, cancellationToken);

        await tenantSeeder.SeedAsync(
            ToTenantInfo(tenant),
            command.AdminEmail,
            command.AdminPassword,
            command.AdminFirstName,
            command.AdminLastName,
            cancellationToken);

        return tenant.Id;
    }

    private async Task<Result<Tenant>> CreateCatalogEntry(ProvisionTenantCommand command, string subdomain, CancellationToken cancellationToken)
    {
        Plan? plan = await platform.Plans
            .FirstOrDefaultAsync(p => p.Id == command.PlanId && p.IsActive, cancellationToken);

        if (plan is null)
        {
            return PlanErrors.NotFound(command.PlanId);
        }

        var now = new DateTimeOffset(dateTimeProvider.UtcNow, TimeSpan.Zero);
        DateTimeOffset? trialEndsAt = command.StartWithTrial ? now.AddDays(TrialDays) : null;

        Result<Tenant> tenantResult = Tenant.Create(command.Name, subdomain, plan.Id, now, trialEndsAt);
        if (tenantResult.IsError)
        {
            return tenantResult.Errors;
        }

        Tenant tenant = tenantResult.Value;

        if (!command.StartWithTrial)
        {
            DateTimeOffset expiresAt = command.Interval == BillingInterval.Annual ? now.AddYears(1) : now.AddMonths(1);
            decimal price = command.Interval == BillingInterval.Annual ? plan.AnnualPrice : plan.MonthlyPrice;

            Result<Subscription> subscription = Subscription.Create(
                tenant.Id, plan.Id, command.Interval, price, plan.Currency, now, expiresAt);

            if (subscription.IsError)
            {
                return subscription.Errors;
            }

            tenant.RenewSubscription(expiresAt);
            platform.Subscriptions.Add(subscription.Value);
        }

        platform.Tenants.Add(tenant);
        await platform.SaveChangesAsync(cancellationToken);

        return tenant;
    }

    private static TenantInfo ToTenantInfo(Tenant tenant) => new(
        tenant.Id,
        tenant.Subdomain,
        tenant.SchemaName,
        tenant.Status,
        tenant.ConnectionString,
        tenant.TrialEndsAtUtc,
        tenant.SubscriptionExpiresAtUtc);
}
