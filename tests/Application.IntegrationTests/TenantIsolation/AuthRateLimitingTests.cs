using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Application.IntegrationTests.TenantIsolation;

/// <summary>
/// M7-B2: the strict login rate limiter (LoginLimiter) rejects the first request past the
/// per-IP window with 429, while health probes â€” which carry no rate-limit policy â€” are
/// never throttled. Runs on the shared migrated/throwaway-DB factory with the permit
/// limit overridden down to 3 so the window trips quickly.
/// </summary>
public sealed class AuthRateLimitingWebApplicationFactory : MultiTenantWebApplicationFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Login:PermitLimit"] = "3",
            });
        });
    }
}

public sealed class AuthRateLimitingTests : IClassFixture<AuthRateLimitingWebApplicationFactory>
{
    private readonly AuthRateLimitingWebApplicationFactory _factory;

    public AuthRateLimitingTests(AuthRateLimitingWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ExceedingPermitLimit_Returns429WithRetryAfter()
    {
        using HttpClient client = _factory.CreateClient();

        for (int attempt = 0; attempt < 3; attempt++)
        {
            HttpResponseMessage denied = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { email = "nobody@goldstore.test", password = "wrong-password" });
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        }

        HttpResponseMessage limited = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "nobody@goldstore.test", password = "wrong-password" });

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal(60, limited.Headers.RetryAfter?.Delta?.TotalSeconds);
    }

    [Fact]
    public async Task HealthProbe_IsNeverThrottled()
    {
        using HttpClient client = _factory.CreateClient();

        // Exceeds the login window on purpose: health endpoints carry no rate-limit
        // policy, so all probes must pass regardless of the login budget.
        for (int attempt = 0; attempt < 5; attempt++)
        {
            HttpResponseMessage response = await client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
