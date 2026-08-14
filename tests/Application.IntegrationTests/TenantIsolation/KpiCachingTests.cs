using System.Net;
using System.Text.Json;
using Application.Abstractions.Messaging;
using Application.Features.Inventory.Adjustments.Create;
using Domain.Inventory;
using Domain.Tenants;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Result;
using Xunit;

namespace Application.IntegrationTests.TenantIsolation;

/// <summary>
/// M7-C1: KPI queries are cached per tenant and evicted when the tenant writes ledger
/// data. A ledger-affecting inventory adjustment (gold IN entry) must be reflected by the
/// next KPI read; the write-driven eviction is the cache-correctness backstop.
/// </summary>
public sealed class KpiCachingTests : IClassFixture<MultiTenantWebApplicationFactory>
{
    private const string TenantAAdmin = "admin@goldstore";
    private const string TenantAAdminPassword = "GoldStore@321!";

    private readonly MultiTenantWebApplicationFactory _factory;

    public KpiCachingTests(MultiTenantWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InventoryKpi_ReflectsLedgerWriteAfterEviction()
    {
        using HttpClient client = await _factory.CreateAuthenticatedClientAsync(TenantAAdmin, TenantAAdminPassword);

        decimal before = await GetInventoryTotalAsync(client);

        var (scope, _) = await _factory.OpenTenantContextAsync(InitialTenant.Id, InitialTenant.Key);
        using (scope)
        {
            ICommandHandler<CreateInventoryAdjustmentCommand, Guid> handler =
                scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateInventoryAdjustmentCommand, Guid>>();

            Result<Guid> result = await handler.Handle(
                new CreateInventoryAdjustmentCommand(
                    AdjustmentType: (int)InventoryAdjustmentType.Increase,
                    Karat: 21,
                    WeightInGrams: 10m,
                    Reason: "KPI eviction test",
                    Notes: null,
                    Date: DateTime.Now),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
        }

        decimal after = await GetInventoryTotalAsync(client);

        Assert.Equal(before + 10m, after);
    }

    private async Task<decimal> GetInventoryTotalAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.GetAsync("/Inventory/GetKpis");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("totalEquivalent21KGrams").GetDecimal();
    }
}
