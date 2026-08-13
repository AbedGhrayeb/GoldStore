using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Application.IntegrationTests.TenantIsolation;

/// <summary>
/// Phase 8 items 2-3: HTTP-level tenant isolation. Each tenant logs in with its own
/// admin and the direct-ID attack is attempted with the other tenant's entity ids.
/// </summary>
public sealed class MvcTenantIsolationHttpTests : IClassFixture<MultiTenantWebApplicationFactory>
{
    private const string TenantAAdmin = "admin@goldstore";
    private const string TenantAAdminPassword = "GoldStore@321!";
    private const string TenantBAdmin = "admin@second-store.goldstore.test";

    private readonly MultiTenantWebApplicationFactory _factory;

    public MvcTenantIsolationHttpTests(MultiTenantWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_ReturnsOnlyOwnSuppliers()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(TenantAAdmin, TenantAAdminPassword);

        JsonElement[] suppliers = await client.GetFromJsonAsync<JsonElement[]>("/Suppliers/List");

        Assert.NotEmpty(suppliers);
        Assert.All(suppliers, s => Assert.NotEqual(_factory.SupplierB.Id.ToString(), s.GetProperty("id").GetString()));
        Assert.Contains(suppliers, s => s.GetProperty("id").GetString() == _factory.SupplierA.Id.ToString());
    }

    [Fact]
    public async Task GetById_CrossTenantId_IsNotExposed()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(TenantAAdmin, TenantAAdminPassword);

        HttpResponseMessage response = await client.GetAsync($"/Suppliers/GetById?id={_factory.SupplierB.Id}");
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"success\":false", body);
    }

    [Fact]
    public async Task GetById_OwnId_IsVisible()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(TenantBAdmin, MultiTenantWebApplicationFactory.TestPassword);

        HttpResponseMessage response = await client.GetAsync($"/Suppliers/GetById?id={_factory.SupplierB.Id}");
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"success\":true", body);
        Assert.Contains(_factory.SupplierB.Name, body);
    }

    [Fact]
    public async Task CrossTenantEditAttempt_IsRejected()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync(TenantAAdmin, TenantAAdminPassword);

        // A's admin cannot load B's supplier into the edit form (ToastResult Error = 0)...
        HttpResponseMessage modal = await client.GetAsync($"/Suppliers/AddOrUpdate?id={_factory.SupplierB.Id}");
        Assert.Contains("\"status\":0", await modal.Content.ReadAsStringAsync());

        // ...and a direct POST against B's supplier id fails too.
        string? token = await TestAuth.GetAntiforgeryTokenAsync(client, "/Suppliers/AddOrUpdate");
        Assert.NotNull(token);

        using FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["Id"] = _factory.SupplierB.Id.ToString(),
            ["Name"] = "Hacked",
            ["PrimaryPhone"] = "0799999999",
            ["SecondaryPhone"] = "",
            ["BankAccountNumber"] = "1234567890",
            ["Notes"] = "",
            ["IsActive"] = "true",
            ["__RequestVerificationToken"] = token,
        });

        HttpResponseMessage response = await client.PostAsync("/Suppliers/Edit", form);
        string body = await response.Content.ReadAsStringAsync();

        // The edit action returns a ToastResult; Error status is 0 (JsonStatus.Error).
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":0", body);
        Assert.DoesNotContain(_factory.SupplierB.Name, body);
    }

    [Fact]
    public async Task UnauthenticatedRequest_RedirectsToLogin()
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        HttpResponseMessage response = await client.GetAsync("/Suppliers/List");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
