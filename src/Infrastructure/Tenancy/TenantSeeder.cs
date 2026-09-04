using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Tenancy;
using Domain.Common;
using Domain.Employees;
using Domain.Finance;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Result;

namespace Infrastructure.Tenancy;

internal sealed class TenantSeeder(
    IServiceScopeFactory scopeFactory,
    IPasswordHasher passwordHasher) : ITenantSeeder
{
    public async Task SeedAsync(
        TenantInfo tenant,
        string adminEmail,
        string adminPassword,
        string adminFirstName,
        string adminLastName,
        CancellationToken cancellationToken = default)
    {
        // Dedicated scope: the tenant context is established explicitly here because
        // provisioning runs outside any tenant HTTP request.
        using IServiceScope scope = scopeFactory.CreateScope();

        ITenantContextSetter tenantContextSetter = scope.ServiceProvider.GetRequiredService<ITenantContextSetter>();
        tenantContextSetter.Set(tenant, isReadOnly: false);

        IApplicationDbContext context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        if (!await context.Users.AnyAsync(cancellationToken))
        {
            string hashedPassword = passwordHasher.Hash(adminPassword);
            Result<User> adminUser = User.Create(
                Guid.CreateVersion7(),
                adminEmail,
                adminFirstName,
                adminLastName,
                hashedPassword,
                RoleEnum.Admin.ToString());

            context.Users.Add(adminUser.Value);
        }

        if (!await context.FinancialAccounts.AnyAsync(cancellationToken))
        {
            var seedAccounts = new List<FinancialAccount>();

            foreach ((string name, Currency currency, string notes) in new[]
            {
                ("صندوق الدينار الرئيسي", Currency.JOD, "صندوق الدينار الكاش الرئيسي"),
                ("صندوق الدولار الرئيسي", Currency.USD, "صندوق الدولار الكاش الرئيسي"),
                ("صندوق الشيكل الرئيسي", Currency.ILS, "صندوق الشيكل الكاش الرئيسي")
            })
            {
                Result<FinancialAccount> account = FinancialAccount.Create(
                    name,
                    currency,
                    FinancialAccountType.Cash,
                    $"{currency}-{Random.Shared.Next(1000, 9999)}",
                    notes);

                seedAccounts.Add(account.Value);
            }

            context.FinancialAccounts.AddRange(seedAccounts);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
