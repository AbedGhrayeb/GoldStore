// <copyright file="ProvisionTenantCommandHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Authorization;
using Domain.Common;
using Domain.Finance;
using Domain.Tenants;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Tenants.Provision;

/// <summary>
/// Provisions a new store (plan Phase 5 item 7). All tenant-owned rows are created in
/// one SaveChanges, so the tenant, settings, subscription, administrator, role
/// assignment, and seed accounts commit atomically or not at all. The ambient tenant
/// is selected explicitly before the write guard runs.
/// </summary>
internal sealed class ProvisionTenantCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ICurrentTenantSetter currentTenantSetter)
    : ICommandHandler<ProvisionTenantCommand, Guid>
{
    private const string StoreAdministratorRoleKey = "store_admin";

    public async Task<Result<Guid>> Handle(ProvisionTenantCommand command, CancellationToken cancellationToken)
    {
        string normalizedKey = command.Key.Trim().ToLowerInvariant();

        // Tenant keys are unique system-wide. Tenant is global reference data (no query
        // filter), so the uniqueness check must be explicit.
        if (await context.Tenants.AsNoTracking().AnyAsync(t => t.Key == normalizedKey, cancellationToken))
        {
            return TenantErrors.KeyNotUnique;
        }

        SubscriptionPlan? plan = await context.SubscriptionPlans
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == command.SubscriptionPlanId, cancellationToken);
        if (plan is null)
        {
            return TenantErrors.PlanNotFound(command.SubscriptionPlanId);
        }

        Role? adminRole = await context.Roles
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Key == StoreAdministratorRoleKey, cancellationToken);
        if (adminRole is null)
        {
            return TenantErrors.StoreAdministratorRoleNotFound;
        }

        // Emails are unique system-wide (plan Phase 0 item 6), so the check must bypass
        // the tenant query filter.
        if (await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == command.AdminEmail, cancellationToken))
        {
            return UserErrors.EmailNotUnique;
        }

        Result<Tenant> tenantResult = Tenant.Create(command.Name, normalizedKey, TenantStatus.Active);
        if (tenantResult.IsError)
        {
            return tenantResult.Errors;
        }

        Tenant tenant = tenantResult.Value;
        context.Tenants.Add(tenant);

        // Select the tenant explicitly before writing tenant-owned rows so the write
        // guard stamps TenantId and the global query filters match on later reads.
        currentTenantSetter.Set(tenant.Id, tenant.Key);

        Result<TenantSettings> settingsResult = TenantSettings.Create(
            tenant.Id, command.Name, logoUrl: null, command.TimeZoneId, enabledFeatures: [.. Domain.Tenants.Features.All]);
        if (settingsResult.IsError)
        {
            return settingsResult.Errors;
        }

        context.TenantSettings.Add(settingsResult.Value);

        // Auto-calculate billing cycle and end date from plan duration; start defaults to provided StartsAtUtc (frontend defaults to today)
        DateTimeOffset startsAt = command.StartsAtUtc == default ? DateTimeOffset.UtcNow : command.StartsAtUtc;
        DateTimeOffset endsAt = startsAt.AddMonths(plan.DurationInMonths);
        SubscriptionBillingCycle billingCycle = plan.DurationInMonths >= 12 ? SubscriptionBillingCycle.Annual : SubscriptionBillingCycle.Monthly;

        Result<TenantSubscription> subscriptionResult = TenantSubscription.Create(
            tenant.Id, plan.Id, billingCycle, startsAt, endsAt);
        if (subscriptionResult.IsError)
        {
            return subscriptionResult.Errors;
        }

        context.TenantSubscriptions.Add(subscriptionResult.Value);

        Result<User> userResult = User.Create(
            Guid.CreateVersion7(), tenant.Id, command.AdminEmail, command.AdminFirstName,
            command.AdminLastName, passwordHasher.Hash(command.AdminPassword), command.AdminPhoneNumber, command.AdminWhatsappNumber);
        if (userResult.IsError)
        {
            return userResult.Errors;
        }

        context.Users.Add(userResult.Value);

        Result<UserRole> userRoleResult = UserRole.Create(tenant.Id, userResult.Value.Id, adminRole.Id);
        if (userRoleResult.IsError)
        {
            return userRoleResult.Errors;
        }

        context.UserRoles.Add(userRoleResult.Value);

        this.SeedDefaultFinancialAccounts();

        await context.SaveChangesAsync(cancellationToken);

        return tenant.Id;
    }

    private void SeedDefaultFinancialAccounts()
    {
        context.FinancialAccounts.Add(FinancialAccount.Create(
            "صندوق الدينار الرئيسي", Currency.JOD, FinancialAccountType.Cash,
            "JOD-" + Random.Shared.Next(1000, 9999), "صندوق الدينار الكاش الرئيسي").Value);
        context.FinancialAccounts.Add(FinancialAccount.Create(
            "صندوق الدولار الرئيسي", Currency.USD, FinancialAccountType.Cash,
            "USD-" + Random.Shared.Next(1000, 9999), "صندوق الدولار الكاش الرئيسي").Value);
        context.FinancialAccounts.Add(FinancialAccount.Create(
            "صندوق الشيكل الرئيسي", Currency.ILS, FinancialAccountType.Cash,
            "ILS-" + Random.Shared.Next(1000, 9999), "صندوق الشيكل الكاش الرئيسي").Value);
    }
}
