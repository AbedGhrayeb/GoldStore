using Application.Abstractions.Authentication;
using Domain.Common;
using Domain.Finance;
using Domain.Inventory;
using Domain.SupplierOperations;
using Domain.Users;
using Infrastructure.Database;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data;

public class ApplicationDbContextInitializer(
    ILogger<ApplicationDbContextInitializer> logger,
    ApplicationDbContext context,IConfiguration configuration,IPasswordHasher passwordHasher)
{
    private readonly ILogger<ApplicationDbContextInitializer> _logger = logger;
    private readonly ApplicationDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    public async Task InitializeAsync()
    {
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        try
        {
            await _context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
    }

    public async Task SeedAsync()
    {
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
    }

    public async Task TrySeedAsync()
    {
        // Default users
        if(! _context.Users.Any())
        {
            string defaultPassword=_configuration["DefaultUserPassword"]!;
            string hashedPassword=_passwordHasher.Hash(defaultPassword);
            var defaultUsers=new List<User>
            {
                new User
                {
                    Id=Guid.CreateVersion7(),
                    Email="abed@sarhangold.ps",
                    FirstName="Abdulrahman",
                    LastName="Ghrayeb",
                    PasswordHash=hashedPassword
                },
                 new User
                {
                    Id=Guid.CreateVersion7(),
                    Email="ramzi@sarhangold.ps",
                    FirstName="Ramzi",
                    LastName="Sarhan",
                    PasswordHash=hashedPassword
                }, new User
                {
                    Id=Guid.CreateVersion7(),
                    Email="tareq@sarhangold.ps",
                    FirstName="Tareq",
                    LastName="Sarhan",
                    PasswordHash=hashedPassword
                }, new User
                {
                    Id=Guid.CreateVersion7(),
                    Email="yazan@sarhangold.ps",
                    FirstName="Yazan",
                    LastName="Sarhan",
                    PasswordHash=hashedPassword
                },
        };
            _context.Users.AddRange(defaultUsers);
        }
        if (!_context.FinancialAccounts.Any())
        {
            var seedAccounts = new List<FinancialAccount>
            {
                new()
                {
                    Id = Guid.CreateVersion7(),
                    Name = "صندوق الدينار الرئيسي",
                    Currency = Currency.Jod,
                    AccountType = FinancialAccountType.Cash,
                    AccountNumber = "JOD-" + Random.Shared.Next(1000, 9999),
                    Notes = "صندوق الدينار الكاش الرئيسي",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.CreateVersion7(),
                    Name = "صندوق الدولار الرئيسي",
                    Currency = Currency.Usd,
                    AccountType = FinancialAccountType.Cash,
                    AccountNumber = "USD-" + Random.Shared.Next(1000, 9999),
                    Notes = "صندوق الدولار الكاش الرئيسي",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.CreateVersion7(),
                    Name = "صندوق الشيكل الرئيسي",
                    Currency = Currency.Ils,
                    AccountType = FinancialAccountType.Cash,
                    AccountNumber = "ILS-" + Random.Shared.Next(1000, 9999),
                    Notes = " صندوق الشيكل الكاش الرئيسي",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
            };
            _context.FinancialAccounts.AddRange(seedAccounts);
        }

       // await BackfillGoldLedgerEntriesAsync();

        await _context.SaveChangesAsync();

    }
    private async Task BackfillGoldLedgerEntriesAsync()
    {
        HashSet<Guid> existingRefIds = await _context.GoldLedgerEntries
            .Where(e => e.ReferenceType == GoldReferenceType.SupplierDelivery && e.ReferenceId.HasValue)
            .Select(e => e.ReferenceId!.Value)
            .ToHashSetAsync();

        List<SupplierDelivery> orphanDeliveries = await _context.SupplierDeliveries
            .Where(d => !existingRefIds.Contains(d.Id))
            .ToListAsync();

        if (orphanDeliveries.Count == 0)
        {
            return;
        }

        Guid userId = await _context.Users.Select(u => u.Id).FirstOrDefaultAsync();
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("No user found to assign backfilled GoldLedgerEntries.");
            return;
        }

        var backfillEntries = orphanDeliveries.Select(d => new GoldLedgerEntry
        {
            Id = Guid.NewGuid(),
            Karat = d.Karat,
            WeightInGrams = d.WeightInGrams,
            Equivalent21KWeightInGrams = d.Equivalent21KWeightInGrams,
            MovementType = GoldMovementType.Increase,
            ReferenceType = GoldReferenceType.SupplierDelivery,
            ReferenceId = d.Id,
            UserId = userId,
            Date = d.Date,
            Notes = d.Notes
        }).ToList();

        _context.GoldLedgerEntries.AddRange(backfillEntries);

        _logger.LogInformation("Backfilled {Count} GoldLedgerEntry records from existing SupplierDeliveries.", backfillEntries.Count);
    }

}

public static class InitializerExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        ApplicationDbContextInitializer initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();

        await initializer.InitializeAsync();

        await initializer.SeedAsync();
    }
}

