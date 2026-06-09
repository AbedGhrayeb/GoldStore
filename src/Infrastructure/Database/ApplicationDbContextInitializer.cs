using Application.Abstractions.Authentication;
using Domain.Users;
using Infrastructure.Database;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
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
        await _context.SaveChangesAsync();

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

