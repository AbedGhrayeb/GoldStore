using Application.Abstractions.Authentication;
using Domain.Common;
using Domain.Employees;
using Domain.Finance;
using Domain.Users;
using Infrastructure.Database;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Result;

namespace Infrastructure.Data;

public class ApplicationDbContextInitializer(
    ILogger<ApplicationDbContextInitializer> logger,
    ApplicationDbContext context, IConfiguration configuration, IPasswordHasher passwordHasher)
{
    private readonly ILogger<ApplicationDbContextInitializer> _logger = logger;
    private readonly ApplicationDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        try
        {
            await _context.Database.EnsureCreatedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        try
        {
            await TrySeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
    }

    public async Task TrySeedAsync(CancellationToken cancellationToken = default)
    {
        // Default users
        if (!_context.Users.Any())
        {
            string defaultPassword = _configuration["DefaultUserPassword"]!;
            string hashedPassword = _passwordHasher.Hash(defaultPassword);
            Result<User> defaultUser = User.Create(Guid.CreateVersion7(), "admin@goldstore", "Admin", "Admin", hashedPassword, RoleEnum.Admin.ToString());

            _context.Users.Add(defaultUser.Value);
        }
        if (!_context.FinancialAccounts.Any())
        {
            var seedAccounts = new List<FinancialAccount>();

            Result<FinancialAccount> jodAccount = FinancialAccount.Create("صندوق الدينار الرئيسي", Currency.JOD, FinancialAccountType.Cash, "JOD-" + Random.Shared.Next(1000, 9999), "صندوق الدينار الكاش الرئيسي");
            Result<FinancialAccount> usdAccount = FinancialAccount.Create("صندوق الدولار الرئيسي", Currency.USD, FinancialAccountType.Cash, "JOD-" + Random.Shared.Next(1000, 9999), "صندوق الدولار الكاش الرئيسي");
            Result<FinancialAccount> ilsAccount = FinancialAccount.Create("صندوق الدينار الرئيسي", Currency.ILS, FinancialAccountType.Cash, "JOD-" + Random.Shared.Next(1000, 9999), "صندوق الشيكل الكاش الرئيسي");

            seedAccounts.AddRange(jodAccount.Value, usdAccount.Value, ilsAccount.Value);


            _context.FinancialAccounts.AddRange(seedAccounts);
        }

        await _context.SaveChangesAsync(cancellationToken);

    }

}

public static class InitializerExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = app.Services.CreateScope();

        ApplicationDbContextInitializer initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();

        await initializer.InitializeAsync(cancellationToken);

        await initializer.SeedAsync(cancellationToken);
    }
}

