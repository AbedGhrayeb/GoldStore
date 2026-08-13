using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Application.IntegrationTests.TenantIsolation;

/// <summary>
/// Phase 8 item 6: login eligibility, disabled users, pending tenants, cancellation
/// grace read-only enforcement, and host-only separation.
/// </summary>
public sealed class AuthEligibilityTests : IClassFixture<MultiTenantWebApplicationFactory>
{
    // Initial tenant admin seeded by the app initializer (src/WebUI/appsettings.json).
    private const string TenantAAdmin = "admin@goldstore";
    private const string TenantAAdminPassword = "GoldStore@321!";
    private const string PendingAdmin = "admin@pending-store.goldstore.test";
    private const string GraceAdmin = "admin@grace-store.goldstore.test";
    private const string ExpiredGraceAdmin = "admin@expired-store.goldstore.test";
    private const string DisabledUser = "disabled@goldstore.test";

    private readonly MultiTenantWebApplicationFactory _factory;

    public AuthEligibilityTests(MultiTenantWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ValidTenantAndUser_ReturnsToken()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = TenantAAdmin, password = TenantAAdminPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("accessToken").GetString()));
    }

    [Theory]
    [InlineData(PendingAdmin)]
    [InlineData(ExpiredGraceAdmin)]
    [InlineData(DisabledUser)]
    public async Task Login_DisqualifiedUserOrTenant_IsDenied(string email)
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = MultiTenantWebApplicationFactory.TestPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_IsDenied()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = TenantAAdmin, password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CancelledTenantInsideGrace_CanReadButNotWrite()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(GraceAdmin, MultiTenantWebApplicationFactory.TestPassword);

        HttpResponseMessage read = await client.GetAsync("/Suppliers/List");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);

        HttpResponseMessage write = await client.PostAsync("/Suppliers/AddOrUpdateAjax",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = "Read Only Shop",
                ["PrimaryPhone"] = "0791234567",
                ["__RequestVerificationToken"] = "",
            }));
        string body = await write.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        Assert.Contains("tenant.read_only", body);
    }

    [Fact]
    public async Task TenantUser_CannotReachHostAdministration()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(TenantAAdmin, TenantAAdminPassword);
        // Simulate a browser navigation so the cookie challenge redirects instead of
        // returning 401 (the cookie handler treats header-less requests as API calls).
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");

        HttpResponseMessage response = await client.GetAsync("/host/tenants");

        // Host administration is protected by the dedicated host cookie; a store user is
        // not authenticated for that scheme. The cookie challenge redirects browsers to
        // the host login, and returns 401 with the host login location for API clients.
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Unauthorized,
            $"Expected a host-login challenge, got {(int)response.StatusCode}.");
        Assert.Contains("/host/login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
