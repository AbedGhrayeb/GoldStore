// <copyright file="AuthLockoutTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using Application.Abstractions.Authentication;
using Domain.Tenants;
using Domain.Users;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Application.IntegrationTests.TenantIsolation;

/// <summary>
/// M7-B1: account lockout on both store (User) and host (PlatformUser) sign-in.
/// Five consecutive failures lock the account; the correct password is rejected while
/// locked, a success below the threshold resets the counter, and an expired lockout
/// lifts. Each test seeds its own user so the shared fixture database stays isolated.
/// </summary>
public sealed class AuthLockoutTests : IClassFixture<MultiTenantWebApplicationFactory>
{
    private readonly MultiTenantWebApplicationFactory factory;

    public AuthLockoutTests(MultiTenantWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task StoreLogin_FiveWrongAttempts_LocksAccountEvenForCorrectPassword()
    {
        string email = await this.CreateStoreUserAsync();

        using HttpClient client = this.factory.CreateClient();

        for (int attempt = 0; attempt < 5; attempt++)
        {
            HttpResponseMessage denied = await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { email, password = "wrong-password" });
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        }

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = MultiTenantWebApplicationFactory.TestPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StoreLogin_FourWrongAttempts_ThenCorrectPassword_SignsInAndResetsCounter()
    {
        string email = await this.CreateStoreUserAsync();

        using HttpClient client = this.factory.CreateClient();

        for (int attempt = 0; attempt < 4; attempt++)
        {
            HttpResponseMessage denied = await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { email, password = "wrong-password" });
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        }

        HttpResponseMessage success = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = MultiTenantWebApplicationFactory.TestPassword });
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);

        (IServiceScope scope, ApplicationDbContext db) = this.factory.OpenNoTenantContext();
        using (scope)
        {
            User user = db.Users.IgnoreQueryFilters().Single(u => u.Email == email);
            Assert.Equal(0, user.FailedLoginAttempts);
            Assert.Null(user.LockedUntilUtc);
        }
    }

    [Fact]
    public async Task StoreLogin_ExpiredLockout_AllowsSignIn()
    {
        string email = await this.CreateStoreUserAsync();

        using HttpClient client = this.factory.CreateClient();

        for (int attempt = 0; attempt < 5; attempt++)
        {
            await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { email, password = "wrong-password" });
        }

        (IServiceScope scope, ApplicationDbContext db) = this.factory.OpenNoTenantContext();
        using (scope)
        {
            await db.Users.IgnoreQueryFilters()
                .Where(u => u.Email == email)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.LockedUntilUtc, DateTimeOffset.UtcNow.AddMinutes(-1)));
        }

        HttpResponseMessage success = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = MultiTenantWebApplicationFactory.TestPassword });
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
    }

    [Fact]
    public async Task HostLogin_FiveWrongAttempts_LocksPlatformAccount()
    {
        string email = await this.CreatePlatformUserAsync();

        using HttpClient client = this.factory.CreateClient();

        for (int attempt = 0; attempt < 5; attempt++)
        {
            HttpResponseMessage denied = await client.PostAsJsonAsync(
                "/host/api/v1/auth/login",
                new { email, password = "wrong-password" });
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        }

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/host/api/v1/auth/login",
            new { email, password = MultiTenantWebApplicationFactory.TestPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> CreateStoreUserAsync()
    {
        string email = $"lockout-{Guid.NewGuid():N}@goldstore.test";
        (IServiceScope scope, ApplicationDbContext db) = await this.factory.OpenTenantContextAsync(InitialTenant.Id, InitialTenant.Key);
        using (scope)
        {
            IPasswordHasher hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            User user = User.Create(Guid.CreateVersion7(), InitialTenant.Id, email, "Lockout", "Test",
                hasher.Hash(MultiTenantWebApplicationFactory.TestPassword)).Value;
            db.Users.Add(user);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        return email;
    }

    private async Task<string> CreatePlatformUserAsync()
    {
        string email = $"lockout-{Guid.NewGuid():N}@host.goldstore.test";
        using IServiceScope scope = this.factory.Services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        IPasswordHasher hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        db.PlatformUsers.Add(Domain.Tenants.PlatformUser.Create(email, "Lockout", "Host",
            hasher.Hash(MultiTenantWebApplicationFactory.TestPassword)).Value);
        await db.SaveChangesAsync(CancellationToken.None);
        return email;
    }
}
