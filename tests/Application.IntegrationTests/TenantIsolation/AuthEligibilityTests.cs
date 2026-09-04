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

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login",
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

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password = MultiTenantWebApplicationFactory.TestPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_IsDenied()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = TenantAAdmin, password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CancelledTenantInsideGrace_CanReadButNotWrite()
    {
        HttpClient client = await _factory.CreateJwtClientAsync(GraceAdmin, MultiTenantWebApplicationFactory.TestPassword);

        HttpResponseMessage read = await client.GetAsync("/api/v1/suppliers");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);

        using var payload = JsonContent.Create(new
        {
            name = "Read Only Shop",
            primaryPhone = "0791234567",
            secondaryPhone = (string?)null,
            bankAccountNumber = "JO00READONLY",
            notes = "grace read-only probe",
            isActive = true
        });
        HttpResponseMessage write = await client.PostAsync("/api/v1/suppliers", payload);
        string body = await write.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        Assert.Contains("tenant.read_only", body);
    }

    [Fact]
    public async Task TenantUser_CannotReachHostAdministration()
    {
        // Tenant JWT must not grant access to host administration.
        HttpClient client = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);

        // Host administration is guarded by the dedicated host JWT (HttpOnly cookie). A store
        // user's tenant credentials are never accepted there: the host JWT bearer scheme
        // challenges with a plain 401 — no redirect and no server-rendered login page (the
        // Angular client renders /host/login itself).
        HttpResponseMessage response = await client.GetAsync("/host/api/v1/tenants");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Contains(
            response.Headers.WwwAuthenticate,
            header => header.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase));
    }
}
