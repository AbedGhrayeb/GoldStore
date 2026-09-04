using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Common;
using Domain.Debts;
using Domain.Inventory;
using Domain.SupplierOperations;
using Domain.Tenants;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Application.IntegrationTests.TenantIsolation;

/// <summary>
/// Phase 7a (A6-B1/B2/B3/B4/B5): HTTP-level tenant isolation for the tenant API surface
/// (<c>/api/v1/*</c>). Clients sign in through <c>/api/v1/auth/login</c> via the
/// <see cref="MultiTenantWebApplicationFactory.CreateJwtClientAsync"/> helper; every
/// assertion verifies a request scoped to one tenant can never read, write, or mutate
/// another tenant's rows, and that no request parameter can override the caller's tenant.
/// </summary>
public sealed class ApiIsolationTests : IClassFixture<MultiTenantWebApplicationFactory>
{
    private const string TenantAAdmin = "admin@goldstore";
    private const string TenantAAdminPassword = "GoldStore@321!";
    private const string TenantBAdmin = "admin@second-store.goldstore.test";

    private readonly MultiTenantWebApplicationFactory _factory;

    public ApiIsolationTests(MultiTenantWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Categories_AndUsers_Lists_AreTenantScoped()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        HttpClient clientB = await _factory.CreateJwtClientAsync(TenantBAdmin, MultiTenantWebApplicationFactory.TestPassword);

        JsonElement[] categoriesA = (await clientA.GetFromJsonAsync<JsonElement[]>("/api/v1/categories"))!;
        Assert.Contains(categoriesA, c => c.GetProperty("id").GetString() == _factory.CategoryA.Id.ToString());
        Assert.DoesNotContain(categoriesA, c => c.GetProperty("id").GetString() == _factory.CategoryB.Id.ToString());

        JsonElement[] categoriesB = (await clientB.GetFromJsonAsync<JsonElement[]>("/api/v1/categories"))!;
        Assert.Contains(categoriesB, c => c.GetProperty("id").GetString() == _factory.CategoryB.Id.ToString());
        Assert.DoesNotContain(categoriesB, c => c.GetProperty("id").GetString() == _factory.CategoryA.Id.ToString());

        JsonElement[] usersA = (await clientA.GetFromJsonAsync<JsonElement[]>("/api/v1/users"))!;
        Assert.All(usersA, u => Assert.DoesNotContain("second-store", u.GetProperty("email").GetString()));

        JsonElement[] usersB = (await clientB.GetFromJsonAsync<JsonElement[]>("/api/v1/users"))!;
        Assert.Contains(usersB, u => u.GetProperty("email").GetString() == TenantBAdmin);
    }

    [Fact]
    public async Task CrossTenantCategoryId_UpdateDeleteAndToggle_Return404()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        HttpClient clientB = await _factory.CreateJwtClientAsync(TenantBAdmin, MultiTenantWebApplicationFactory.TestPassword);

        using var update = JsonContent.Create(new
        {
            name = "Hacked",
            description = (string?)null,
            parentCategoryId = (Guid?)null,
            isActive = true,
        });

        HttpResponseMessage updateResponse = await clientA.PutAsync(
            $"/api/v1/categories/{_factory.CategoryB.Id}", update);
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);

        HttpResponseMessage toggleResponse = await clientA.PostAsync(
            $"/api/v1/categories/{_factory.CategoryB.Id}/toggle-active", content: null);
        Assert.Equal(HttpStatusCode.NotFound, toggleResponse.StatusCode);

        // The DELETE write path exists on users (categories are deactivated, not deleted),
        // so the delete direct-ID attack targets tenant B's user from tenant A's client.
        JsonElement[] usersB = (await clientB.GetFromJsonAsync<JsonElement[]>("/api/v1/users"))!;
        string tenantBUserId = usersB
            .Single(u => u.GetProperty("email").GetString() == TenantBAdmin)
            .GetProperty("id")
            .GetString()!;

        HttpResponseMessage deleteResponse = await clientA.DeleteAsync($"/api/v1/users/{tenantBUserId}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task WriteStamping_BogusTenantIdInBody_IsIgnored()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        HttpClient clientB = await _factory.CreateJwtClientAsync(TenantBAdmin, MultiTenantWebApplicationFactory.TestPassword);

        using var payload = JsonContent.Create(new
        {
            name = "عقود",
            description = "stamping probe",
            parentCategoryId = (Guid?)null,
            isActive = true,
            tenantId = _factory.TenantBId.ToString(),
        });

        HttpResponseMessage create = await clientA.PostAsync("/api/v1/categories", payload);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        string categoryId = (await create.Content.ReadFromJsonAsync<string>())!;

        JsonElement[] categoriesB = (await clientB.GetFromJsonAsync<JsonElement[]>("/api/v1/categories"))!;
        Assert.DoesNotContain(categoriesB, c => c.GetProperty("id").GetString() == categoryId);

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Guid actualTenantId = await db.Categories
            .IgnoreQueryFilters()
            .Where(category => category.Id == Guid.Parse(categoryId))
            .Select(category => category.TenantId)
            .SingleAsync();

        Assert.Equal(InitialTenant.Id, actualTenantId);
    }

    [Fact]
    public async Task CategoryName_IsUniqueWithinTenant_ButReusableAcrossTenants()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        HttpClient clientB = await _factory.CreateJwtClientAsync(TenantBAdmin, MultiTenantWebApplicationFactory.TestPassword);

        using var createdInA = JsonContent.Create(new
        {
            name = "أساور",
            description = (string?)null,
            parentCategoryId = (Guid?)null,
            isActive = true,
        });
        HttpResponseMessage created = await clientA.PostAsync("/api/v1/categories", createdInA);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var duplicateInA = JsonContent.Create(new
        {
            name = "أساور",
            description = (string?)null,
            parentCategoryId = (Guid?)null,
            isActive = true,
        });
        HttpResponseMessage conflict = await clientA.PostAsync("/api/v1/categories", duplicateInA);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        using var sameNameInB = JsonContent.Create(new
        {
            name = "أساور",
            description = (string?)null,
            parentCategoryId = (Guid?)null,
            isActive = true,
        });
        HttpResponseMessage createdInB = await clientB.PostAsync("/api/v1/categories", sameNameInB);
        Assert.Equal(HttpStatusCode.Created, createdInB.StatusCode);
    }

    [Fact]
    public async Task FeatureGate_CatalogDisabled_Returns403()
    {
        HttpClient client = await _factory.CreateJwtClientAsync(
            MultiTenantWebApplicationFactory.NoCatalogAdmin, MultiTenantWebApplicationFactory.TestPassword);

        HttpResponseMessage list = await client.GetAsync("/api/v1/categories");
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);

        using var payload = JsonContent.Create(new
        {
            name = "Anything",
            description = (string?)null,
            parentCategoryId = (Guid?)null,
            isActive = true,
        });
        HttpResponseMessage create = await client.PostAsync("/api/v1/categories", payload);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task UsersMe_ReturnsOwnProfile()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);

        JsonElement me = await clientA.GetFromJsonAsync<JsonElement>("/api/v1/users/me");

        Assert.Equal(MultiTenantWebApplicationFactory.ProfileUserEmail, me.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Suppliers_Lists_AreTenantScoped()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        HttpClient clientB = await _factory.CreateJwtClientAsync(TenantBAdmin, MultiTenantWebApplicationFactory.TestPassword);

        JsonElement[] suppliersA = (await clientA.GetFromJsonAsync<JsonElement[]>("/api/v1/suppliers"))!;
        Assert.Contains(suppliersA, supplier => supplier.GetProperty("id").GetString() == _factory.SupplierA.Id.ToString());
        Assert.DoesNotContain(suppliersA, supplier => supplier.GetProperty("id").GetString() == _factory.SupplierB.Id.ToString());

        JsonElement[] suppliersB = (await clientB.GetFromJsonAsync<JsonElement[]>("/api/v1/suppliers"))!;
        Assert.Contains(suppliersB, supplier => supplier.GetProperty("id").GetString() == _factory.SupplierB.Id.ToString());
        Assert.DoesNotContain(suppliersB, supplier => supplier.GetProperty("id").GetString() == _factory.SupplierA.Id.ToString());
    }

    [Fact]
    public async Task CrossTenantSupplierId_ReadUpdateAndToggle_Return404()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);

        HttpResponseMessage readResponse = await clientA.GetAsync($"/api/v1/suppliers/{_factory.SupplierB.Id}");
        Assert.Equal(HttpStatusCode.NotFound, readResponse.StatusCode);

        using var update = JsonContent.Create(new
        {
            name = "Attempted cross-tenant update",
            primaryPhone = "0792222222",
            secondaryPhone = (string?)null,
            bankAccountNumber = "JO00ATTACK",
            notes = "must not be applied",
            isActive = true,
        });
        HttpResponseMessage updateResponse = await clientA.PutAsync(
            $"/api/v1/suppliers/{_factory.SupplierB.Id}", update);
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);

        HttpResponseMessage toggleResponse = await clientA.PostAsync(
            $"/api/v1/suppliers/{_factory.SupplierB.Id}/toggle-active", content: null);
        Assert.Equal(HttpStatusCode.NotFound, toggleResponse.StatusCode);
    }

    [Fact]
    public async Task SupplierDelivery_WriteStamping_IgnoresForgedTenantAndEquivalentWeight()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        const string notes = "api-b2-delivery-stamping";

        using var payload = JsonContent.Create(new
        {
            supplierId = _factory.SupplierA.Id,
            lines = new[]
            {
                new
                {
                    karat = 24,
                    weightInGrams = 12.5M,
                    equivalent21KWeightInGrams = 9999M,
                },
            },
            manufacturingFeePerGram = 0M,
            manufacturingFeeCurrency = "JOD",
            notes,
            tenantId = _factory.TenantBId,
        });

        HttpResponseMessage response = await clientA.PostAsync("/api/v1/supplier-deliveries", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        SupplierDelivery delivery = await db.SupplierDeliveries
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Notes == notes);

        Assert.Equal(InitialTenant.Id, delivery.TenantId);
        Assert.Equal(
            GoldWeight.CalculateEquivalent21KWeight(12.5M, Karat.K24),
            delivery.Equivalent21KWeightInGrams);
        Assert.NotEqual(9999M, delivery.Equivalent21KWeightInGrams);
    }

    [Fact]
    public async Task SupplierName_IsUniqueWithinTenant_ButReusableAcrossTenants()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        HttpClient clientB = await _factory.CreateJwtClientAsync(TenantBAdmin, MultiTenantWebApplicationFactory.TestPassword);
        const string supplierName = "API B2 Supplier";

        using JsonContent createdInA = CreateSupplierPayload(supplierName, "0793333333", "JO00APIA");
        HttpResponseMessage created = await clientA.PostAsync("/api/v1/suppliers", createdInA);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using JsonContent duplicateInA = CreateSupplierPayload(supplierName, "0794444444", "JO00APIB");
        HttpResponseMessage conflict = await clientA.PostAsync("/api/v1/suppliers", duplicateInA);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        using JsonContent sameNameInB = CreateSupplierPayload(supplierName, "0795555555", "JO00APIC");
        HttpResponseMessage createdInB = await clientB.PostAsync("/api/v1/suppliers", sameNameInB);
        Assert.Equal(HttpStatusCode.Created, createdInB.StatusCode);
    }

    [Fact]
    public async Task SupplierFinancialTransaction_UppercaseCurrency_ReturnsPersistedId()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        Guid accountId;

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            accountId = await db.FinancialAccounts
                .IgnoreQueryFilters()
                .Where(account => account.TenantId == InitialTenant.Id && account.Currency == Currency.JOD)
                .Select(account => account.Id)
                .SingleAsync();
        }

        using var payload = JsonContent.Create(new
        {
            supplierId = _factory.SupplierA.Id,
            direction = 1,
            amount = 125M,
            currency = "JOD",
            accountId,
            date = DateTime.UtcNow,
            notes = "api-b2-financial-transaction",
            paymentLegs = (object?)null,
        });

        HttpResponseMessage response = await clientA.PostAsync("/api/v1/supplier-financial-transactions", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Guid returnedId = (await response.Content.ReadFromJsonAsync<Guid>())!;

        JsonElement page = await clientA.GetFromJsonAsync<JsonElement>("/api/v1/supplier-financial-transactions");
        Assert.Contains(
            page.GetProperty("items").EnumerateArray(),
            transaction => transaction.GetProperty("id").GetGuid() == returnedId);
    }

    [Fact]
    public async Task MalformedSupplierCollections_Return400()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);

        using var nullLines = JsonContent.Create(new
        {
            supplierId = _factory.SupplierA.Id,
            lines = (object?)null,
            manufacturingFeePerGram = 0M,
            manufacturingFeeCurrency = "JOD",
            notes = "invalid null lines",
        });
        HttpResponseMessage nullLinesResponse = await clientA.PostAsync("/api/v1/supplier-deliveries", nullLines);
        Assert.Equal(HttpStatusCode.BadRequest, nullLinesResponse.StatusCode);

        using var nullLineItem = JsonContent.Create(new
        {
            supplierId = _factory.SupplierA.Id,
            lines = new object?[] { null },
            manufacturingFeePerGram = 0M,
            manufacturingFeeCurrency = "JOD",
            notes = "invalid null line",
        });
        HttpResponseMessage nullLineResponse = await clientA.PostAsync("/api/v1/supplier-deliveries", nullLineItem);
        Assert.Equal(HttpStatusCode.BadRequest, nullLineResponse.StatusCode);

        using var nullTransactionLeg = JsonContent.Create(new
        {
            supplierId = _factory.SupplierA.Id,
            direction = 1,
            amount = 1M,
            currency = "JOD",
            accountId = Guid.NewGuid(),
            date = DateTime.UtcNow,
            notes = "invalid null payment leg",
            paymentLegs = new object?[] { null },
        });
        HttpResponseMessage transactionResponse = await clientA.PostAsync(
            "/api/v1/supplier-financial-transactions", nullTransactionLeg);
        Assert.Equal(HttpStatusCode.BadRequest, transactionResponse.StatusCode);

        using var nullManufacturingLeg = JsonContent.Create(new
        {
            supplierId = _factory.SupplierA.Id,
            accountId = Guid.NewGuid(),
            amount = 1M,
            currency = "JOD",
            notes = "invalid null payment leg",
            paymentLegs = new object?[] { null },
        });
        HttpResponseMessage manufacturingResponse = await clientA.PostAsync(
            "/api/v1/supplier-payments/manufacturing", nullManufacturingLeg);
        Assert.Equal(HttpStatusCode.BadRequest, manufacturingResponse.StatusCode);
    }

    [Fact]
    public async Task FeatureGates_AreIndependentAndCoverEverySupplierGroup()
    {
        HttpClient noSuppliersClient = await _factory.CreateJwtClientAsync(
            MultiTenantWebApplicationFactory.NoSuppliersAdmin,
            MultiTenantWebApplicationFactory.TestPassword);

        HttpResponseMessage suppliers = await noSuppliersClient.GetAsync("/api/v1/suppliers");
        Assert.Equal(HttpStatusCode.Forbidden, suppliers.StatusCode);

        using var emptyDelivery = JsonContent.Create(new { });
        HttpResponseMessage deliveries = await noSuppliersClient.PostAsync(
            "/api/v1/supplier-deliveries", emptyDelivery);
        Assert.Equal(HttpStatusCode.Forbidden, deliveries.StatusCode);

        using var emptyPayment = JsonContent.Create(new { });
        HttpResponseMessage payments = await noSuppliersClient.PostAsync(
            "/api/v1/supplier-payments/scrap-gold", emptyPayment);
        Assert.Equal(HttpStatusCode.Forbidden, payments.StatusCode);

        HttpResponseMessage financialTransactions = await noSuppliersClient.GetAsync(
            "/api/v1/supplier-financial-transactions");
        Assert.Equal(HttpStatusCode.Forbidden, financialTransactions.StatusCode);

        HttpResponseMessage allowedInventory = await noSuppliersClient.GetAsync("/api/v1/inventory/gold-ledger");
        Assert.Equal(HttpStatusCode.OK, allowedInventory.StatusCode);

        HttpClient noInventoryClient = await _factory.CreateJwtClientAsync(
            MultiTenantWebApplicationFactory.NoInventoryAdmin,
            MultiTenantWebApplicationFactory.TestPassword);

        HttpResponseMessage inventory = await noInventoryClient.GetAsync("/api/v1/inventory/gold-ledger");
        Assert.Equal(HttpStatusCode.Forbidden, inventory.StatusCode);

        HttpResponseMessage allowedSuppliers = await noInventoryClient.GetAsync("/api/v1/suppliers");
        Assert.Equal(HttpStatusCode.OK, allowedSuppliers.StatusCode);
    }

    [Fact]
    public async Task CrossTenantSalesInvoiceId_ReadAndList_Return404OrExcludeInvoice()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        HttpClient clientB = await _factory.CreateJwtClientAsync(TenantBAdmin, MultiTenantWebApplicationFactory.TestPassword);

        using JsonContent payload = CreateSalesInvoicePayload(
            _factory.EmployeeB.Id,
            _factory.FinancialAccountB.Id,
            _factory.CategoryB.Id,
            "api-b3-cross-tenant-sale",
            InitialTenant.Id);
        HttpResponseMessage create = await clientB.PostAsync("/api/v1/sales-invoices", payload);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Guid invoiceId = await create.Content.ReadFromJsonAsync<Guid>();

        HttpResponseMessage directIdAttack = await clientA.GetAsync($"/api/v1/sales-invoices/{invoiceId}");
        Assert.Equal(HttpStatusCode.NotFound, directIdAttack.StatusCode);

        JsonElement ownInvoice = await clientB.GetFromJsonAsync<JsonElement>($"/api/v1/sales-invoices/{invoiceId}");
        Assert.Equal(invoiceId, ownInvoice.GetProperty("id").GetGuid());

        JsonElement pageA = await clientA.GetFromJsonAsync<JsonElement>("/api/v1/sales-invoices");
        Assert.DoesNotContain(
            pageA.GetProperty("items").EnumerateArray(),
            invoice => invoice.GetProperty("id").GetGuid() == invoiceId);
    }

    [Fact]
    public async Task SalesAndPurchaseInvoices_StampCallerTenantAndWriteGoldLedger()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        HttpClient clientB = await _factory.CreateJwtClientAsync(TenantBAdmin, MultiTenantWebApplicationFactory.TestPassword);

        using JsonContent salePayload = CreateSalesInvoicePayload(
            _factory.EmployeeA.Id,
            _factory.FinancialAccountA.Id,
            _factory.CategoryA.Id,
            "api-b3-sale-ledger",
            _factory.TenantBId);
        HttpResponseMessage saleResponse = await clientA.PostAsync("/api/v1/sales-invoices", salePayload);
        Assert.Equal(HttpStatusCode.Created, saleResponse.StatusCode);
        Guid saleId = await saleResponse.Content.ReadFromJsonAsync<Guid>();

        using JsonContent purchasePayload = CreateCustomerPurchaseInvoicePayload(
            _factory.EmployeeB.Id,
            _factory.FinancialAccountB.Id,
            "api-b3-purchase-ledger",
            InitialTenant.Id);
        HttpResponseMessage purchaseResponse = await clientB.PostAsync(
            "/api/v1/customer-purchases/invoices", purchasePayload);
        Assert.Equal(HttpStatusCode.Created, purchaseResponse.StatusCode);
        Guid purchaseId = await purchaseResponse.Content.ReadFromJsonAsync<Guid>();

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Guid persistedSaleTenantId = await db.SalesInvoices
            .IgnoreQueryFilters()
            .Where(invoice => invoice.Id == saleId)
            .Select(invoice => invoice.TenantId)
            .SingleAsync();
        Assert.Equal(InitialTenant.Id, persistedSaleTenantId);

        List<GoldLedgerEntry> saleEntries = await db.GoldLedgerEntries
            .IgnoreQueryFilters()
            .Where(entry => entry.ReferenceId == saleId && entry.ReferenceType == GoldReferenceType.Sale)
            .ToListAsync();
        GoldLedgerEntry saleEntry = Assert.Single(saleEntries);
        Assert.Equal(InitialTenant.Id, saleEntry.TenantId);
        Assert.Equal(GoldMovementType.Decrease, saleEntry.MovementType);

        Guid persistedPurchaseTenantId = await db.CustomerPurchaseInvoices
            .IgnoreQueryFilters()
            .Where(invoice => invoice.Id == purchaseId)
            .Select(invoice => invoice.TenantId)
            .SingleAsync();
        Assert.Equal(_factory.TenantBId, persistedPurchaseTenantId);

        List<GoldLedgerEntry> purchaseEntries = await db.GoldLedgerEntries
            .IgnoreQueryFilters()
            .Where(entry => entry.ReferenceId == purchaseId
                && entry.ReferenceType == GoldReferenceType.CustomerGoldPurchase)
            .ToListAsync();
        GoldLedgerEntry purchaseEntry = Assert.Single(purchaseEntries);
        Assert.Equal(_factory.TenantBId, purchaseEntry.TenantId);
        Assert.Equal(GoldMovementType.Increase, purchaseEntry.MovementType);
    }

    [Fact]
    public async Task PartialPurchase_WithPaymentLeg_UsesValidatedTenantAccountForDebt()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        const string sellerName = "API Partial Purchase Seller";

        using var payload = JsonContent.Create(new
        {
            sellerName,
            sellerPhone = "0792222222",
            sellerIdNumber = "987654321",
            sellerYearOfBirth = 1991,
            sellerAddress = "Amman",
            employeeId = _factory.EmployeeA.Id,
            currency = "JOD",
            date = DateTime.UtcNow,
            totalAmount = 100M,
            amountPaid = 0M,
            paymentMethod = 1,
            accountId = _factory.FinancialAccountB.Id,
            sellerAccountNumber = (string?)null,
            notes = "api-b3-partial-purchase",
            items = new[]
            {
                new
                {
                    categoryId = (Guid?)_factory.CategoryA.Id,
                    karat = 21,
                    weightInGrams = 1M,
                    pricePerGram = 100M,
                },
            },
            paymentLegs = new[]
            {
                new
                {
                    accountId = _factory.FinancialAccountA.Id,
                    currency = "JOD",
                    amount = 40M,
                    exchangeRate = 1M,
                },
            },
            tenantId = _factory.TenantBId,
        });

        HttpResponseMessage response = await clientA.PostAsync("/api/v1/customer-purchases/invoices", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Guid invoiceId = await response.Content.ReadFromJsonAsync<Guid>();

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Debt debt = await db.Debts
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Name == sellerName);

        Assert.Equal(InitialTenant.Id, debt.TenantId);
        Assert.Equal(_factory.FinancialAccountA.Id, debt.AccountId);

        DebtLedgerEntry debtEntry = await db.DebtLedgerEntries
            .IgnoreQueryFilters()
            .SingleAsync(entry => entry.DebtId == debt.Id);
        Assert.Equal(InitialTenant.Id, debtEntry.TenantId);
        Assert.Equal(60M, debtEntry.Amount);

        Assert.True(await db.CustomerPurchaseInvoices
            .IgnoreQueryFilters()
            .AnyAsync(invoice => invoice.Id == invoiceId && invoice.TenantId == InitialTenant.Id));
    }

    [Fact]
    public async Task InvoiceNumbers_AreScopedIndependentlyPerTenantAndModule()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        HttpClient clientB = await _factory.CreateJwtClientAsync(TenantBAdmin, MultiTenantWebApplicationFactory.TestPassword);

        string salesNumberA = (await clientA.GetFromJsonAsync<string>("/api/v1/sales-invoices/next-number"))!;
        string salesNumberB = (await clientB.GetFromJsonAsync<string>("/api/v1/sales-invoices/next-number"))!;

        using (JsonContent payload = CreateSalesInvoicePayload(
            _factory.EmployeeA.Id,
            _factory.FinancialAccountA.Id,
            _factory.CategoryA.Id,
            "api-b3-number-scope-sale",
            _factory.TenantBId))
        {
            HttpResponseMessage create = await clientA.PostAsync("/api/v1/sales-invoices", payload);
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        }

        string nextSalesNumberA = (await clientA.GetFromJsonAsync<string>("/api/v1/sales-invoices/next-number"))!;
        string unchangedSalesNumberB = (await clientB.GetFromJsonAsync<string>("/api/v1/sales-invoices/next-number"))!;
        Assert.Equal(IncrementInvoiceNumber(salesNumberA), nextSalesNumberA);
        Assert.Equal(salesNumberB, unchangedSalesNumberB);

        string purchaseNumberA = (await clientA.GetFromJsonAsync<string>(
            "/api/v1/customer-purchases/invoices/next-number"))!;
        string purchaseNumberB = (await clientB.GetFromJsonAsync<string>(
            "/api/v1/customer-purchases/invoices/next-number"))!;

        using (JsonContent payload = CreateCustomerPurchaseInvoicePayload(
            _factory.EmployeeA.Id,
            _factory.FinancialAccountA.Id,
            "api-b3-number-scope-purchase",
            _factory.TenantBId))
        {
            HttpResponseMessage create = await clientA.PostAsync("/api/v1/customer-purchases/invoices", payload);
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        }

        string nextPurchaseNumberA = (await clientA.GetFromJsonAsync<string>(
            "/api/v1/customer-purchases/invoices/next-number"))!;
        string unchangedPurchaseNumberB = (await clientB.GetFromJsonAsync<string>(
            "/api/v1/customer-purchases/invoices/next-number"))!;
        Assert.Equal(IncrementInvoiceNumber(purchaseNumberA), nextPurchaseNumberA);
        Assert.Equal(purchaseNumberB, unchangedPurchaseNumberB);
    }

    [Fact]
    public async Task ConcurrentInvoiceCreates_AllocateUniqueNumbers()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);

        using JsonContent firstSalePayload = CreateSalesInvoicePayload(
            _factory.EmployeeA.Id,
            _factory.FinancialAccountA.Id,
            _factory.CategoryA.Id,
            "api-b3-concurrent-sale-1",
            _factory.TenantBId);
        using JsonContent secondSalePayload = CreateSalesInvoicePayload(
            _factory.EmployeeA.Id,
            _factory.FinancialAccountA.Id,
            _factory.CategoryA.Id,
            "api-b3-concurrent-sale-2",
            _factory.TenantBId);

        HttpResponseMessage[] saleResponses = await Task.WhenAll(
            clientA.PostAsync("/api/v1/sales-invoices", firstSalePayload),
            clientA.PostAsync("/api/v1/sales-invoices", secondSalePayload));
        Assert.All(saleResponses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));

        Guid[] saleIds = await Task.WhenAll(saleResponses.Select(
            response => response.Content.ReadFromJsonAsync<Guid>()));

        using JsonContent firstPurchasePayload = CreateCustomerPurchaseInvoicePayload(
            _factory.EmployeeA.Id,
            _factory.FinancialAccountA.Id,
            "api-b3-concurrent-purchase-1",
            _factory.TenantBId);
        using JsonContent secondPurchasePayload = CreateCustomerPurchaseInvoicePayload(
            _factory.EmployeeA.Id,
            _factory.FinancialAccountA.Id,
            "api-b3-concurrent-purchase-2",
            _factory.TenantBId);

        HttpResponseMessage[] purchaseResponses = await Task.WhenAll(
            clientA.PostAsync("/api/v1/customer-purchases/invoices", firstPurchasePayload),
            clientA.PostAsync("/api/v1/customer-purchases/invoices", secondPurchasePayload));
        Assert.All(purchaseResponses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));

        Guid[] purchaseIds = await Task.WhenAll(purchaseResponses.Select(
            response => response.Content.ReadFromJsonAsync<Guid>()));

        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        List<string> saleNumbers = await db.SalesInvoices
            .IgnoreQueryFilters()
            .Where(invoice => saleIds.Contains(invoice.Id))
            .Select(invoice => invoice.InvoiceNumber)
            .ToListAsync();
        List<string> purchaseNumbers = await db.CustomerPurchaseInvoices
            .IgnoreQueryFilters()
            .Where(invoice => purchaseIds.Contains(invoice.Id))
            .Select(invoice => invoice.InvoiceNumber)
            .ToListAsync();

        Assert.Equal(2, saleNumbers.Distinct().Count());
        Assert.Equal(2, purchaseNumbers.Distinct().Count());
    }

    [Fact]
    public async Task MalformedInvoiceCollections_Return400()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);

        using var nullSalesItems = JsonContent.Create(new
        {
            customerName = "Invalid Sale",
            date = DateTime.UtcNow,
            currency = "JOD",
            items = (object?)null,
            totalAmount = 100M,
            amountPaid = 100M,
            paymentMethod = 1,
            accountId = _factory.FinancialAccountA.Id,
            employeeId = _factory.EmployeeA.Id,
        });
        HttpResponseMessage nullSalesItemsResponse = await clientA.PostAsync(
            "/api/v1/sales-invoices", nullSalesItems);
        Assert.Equal(HttpStatusCode.BadRequest, nullSalesItemsResponse.StatusCode);

        using var nullSalesItem = JsonContent.Create(new
        {
            customerName = "Invalid Sale",
            date = DateTime.UtcNow,
            currency = "JOD",
            items = new object?[] { null },
            totalAmount = 100M,
            amountPaid = 100M,
            paymentMethod = 1,
            accountId = _factory.FinancialAccountA.Id,
            employeeId = _factory.EmployeeA.Id,
        });
        HttpResponseMessage nullSalesItemResponse = await clientA.PostAsync(
            "/api/v1/sales-invoices", nullSalesItem);
        Assert.Equal(HttpStatusCode.BadRequest, nullSalesItemResponse.StatusCode);

        using var nullSalesLeg = JsonContent.Create(new
        {
            customerName = "Invalid Sale",
            date = DateTime.UtcNow,
            currency = "JOD",
            items = new[] { new { categoryId = (Guid?)null, karat = 21, weightInGrams = 1M, pricePerGram = 100M } },
            totalAmount = 100M,
            amountPaid = 100M,
            paymentMethod = 1,
            accountId = _factory.FinancialAccountA.Id,
            employeeId = _factory.EmployeeA.Id,
            paymentLegs = new object?[] { null },
        });
        HttpResponseMessage nullSalesLegResponse = await clientA.PostAsync(
            "/api/v1/sales-invoices", nullSalesLeg);
        Assert.Equal(HttpStatusCode.BadRequest, nullSalesLegResponse.StatusCode);

        using var nullPurchaseItems = JsonContent.Create(new
        {
            sellerName = "Invalid Purchase",
            sellerIdNumber = "123456789",
            employeeId = _factory.EmployeeA.Id,
            currency = "JOD",
            date = DateTime.UtcNow,
            totalAmount = 100M,
            amountPaid = 100M,
            paymentMethod = 1,
            accountId = _factory.FinancialAccountA.Id,
            items = (object?)null,
        });
        HttpResponseMessage nullPurchaseItemsResponse = await clientA.PostAsync(
            "/api/v1/customer-purchases/invoices", nullPurchaseItems);
        Assert.Equal(HttpStatusCode.BadRequest, nullPurchaseItemsResponse.StatusCode);

        using var nullPurchaseItem = JsonContent.Create(new
        {
            sellerName = "Invalid Purchase",
            sellerIdNumber = "123456789",
            employeeId = _factory.EmployeeA.Id,
            currency = "JOD",
            date = DateTime.UtcNow,
            totalAmount = 100M,
            amountPaid = 100M,
            paymentMethod = 1,
            accountId = _factory.FinancialAccountA.Id,
            items = new object?[] { null },
        });
        HttpResponseMessage nullPurchaseItemResponse = await clientA.PostAsync(
            "/api/v1/customer-purchases/invoices", nullPurchaseItem);
        Assert.Equal(HttpStatusCode.BadRequest, nullPurchaseItemResponse.StatusCode);

        using var nullPurchaseLeg = JsonContent.Create(new
        {
            sellerName = "Invalid Purchase",
            sellerIdNumber = "123456789",
            employeeId = _factory.EmployeeA.Id,
            currency = "JOD",
            date = DateTime.UtcNow,
            totalAmount = 100M,
            amountPaid = 100M,
            paymentMethod = 1,
            accountId = _factory.FinancialAccountA.Id,
            items = new[] { new { categoryId = (Guid?)null, karat = 21, weightInGrams = 1M, pricePerGram = 100M } },
            paymentLegs = new object?[] { null },
        });
        HttpResponseMessage nullPurchaseLegResponse = await clientA.PostAsync(
            "/api/v1/customer-purchases/invoices", nullPurchaseLeg);
        Assert.Equal(HttpStatusCode.BadRequest, nullPurchaseLegResponse.StatusCode);
    }

    [Fact]
    public async Task SalesAndPurchasesFeatureGates_AreIndependent()
    {
        HttpClient noSalesClient = await _factory.CreateJwtClientAsync(
            MultiTenantWebApplicationFactory.NoSalesAdmin,
            MultiTenantWebApplicationFactory.TestPassword);

        HttpResponseMessage salesList = await noSalesClient.GetAsync("/api/v1/sales-invoices");
        Assert.Equal(HttpStatusCode.Forbidden, salesList.StatusCode);
        HttpResponseMessage salesNext = await noSalesClient.GetAsync("/api/v1/sales-invoices/next-number");
        Assert.Equal(HttpStatusCode.Forbidden, salesNext.StatusCode);
        HttpResponseMessage salesKpis = await noSalesClient.GetAsync("/api/v1/sales-invoices/kpis");
        Assert.Equal(HttpStatusCode.Forbidden, salesKpis.StatusCode);
        using var emptySale = JsonContent.Create(new { });
        HttpResponseMessage salesCreate = await noSalesClient.PostAsync("/api/v1/sales-invoices", emptySale);
        Assert.Equal(HttpStatusCode.Forbidden, salesCreate.StatusCode);

        HttpResponseMessage allowedPurchases = await noSalesClient.GetAsync(
            "/api/v1/customer-purchases/invoices/next-number");
        Assert.Equal(HttpStatusCode.OK, allowedPurchases.StatusCode);

        HttpClient noPurchasesClient = await _factory.CreateJwtClientAsync(
            MultiTenantWebApplicationFactory.NoPurchasesAdmin,
            MultiTenantWebApplicationFactory.TestPassword);

        HttpResponseMessage purchaseNext = await noPurchasesClient.GetAsync(
            "/api/v1/customer-purchases/invoices/next-number");
        Assert.Equal(HttpStatusCode.Forbidden, purchaseNext.StatusCode);
        using var emptyPurchase = JsonContent.Create(new { });
        HttpResponseMessage purchaseCreate = await noPurchasesClient.PostAsync(
            "/api/v1/customer-purchases/invoices", emptyPurchase);
        Assert.Equal(HttpStatusCode.Forbidden, purchaseCreate.StatusCode);

        HttpResponseMessage allowedSales = await noPurchasesClient.GetAsync("/api/v1/sales-invoices/next-number");
        Assert.Equal(HttpStatusCode.OK, allowedSales.StatusCode);
    }

    [Fact]
    public async Task FinanceAccountBalances_AreTenantScoped()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        HttpClient clientB = await _factory.CreateJwtClientAsync(TenantBAdmin, MultiTenantWebApplicationFactory.TestPassword);

        using var accountARequest = JsonContent.Create(new
        {
            name = "API B4 Scoped Account",
            currency = "JOD",
            accountNumber = "API-B4-A",
            notes = "tenant A balance probe",
            openingBalance = 1111M,
            tenantId = _factory.TenantBId,
        });
        HttpResponseMessage accountAResponse = await clientA.PostAsync(
            "/api/v1/finance/accounts", accountARequest);
        Assert.Equal(HttpStatusCode.Created, accountAResponse.StatusCode);
        Guid accountAId = await accountAResponse.Content.ReadFromJsonAsync<Guid>();

        using var accountBRequest = JsonContent.Create(new
        {
            name = "API B4 Scoped Account",
            currency = "JOD",
            accountNumber = "API-B4-B",
            notes = "tenant B balance probe",
            openingBalance = 2222M,
            tenantId = InitialTenant.Id,
        });
        HttpResponseMessage accountBResponse = await clientB.PostAsync(
            "/api/v1/finance/accounts", accountBRequest);
        Assert.Equal(HttpStatusCode.Created, accountBResponse.StatusCode);
        Guid accountBId = await accountBResponse.Content.ReadFromJsonAsync<Guid>();

        JsonElement balanceA = await clientA.GetFromJsonAsync<JsonElement>(
            $"/api/v1/finance/accounts/{accountAId}/balance");
        JsonElement balanceB = await clientB.GetFromJsonAsync<JsonElement>(
            $"/api/v1/finance/accounts/{accountBId}/balance");
        Assert.Equal(1111M, balanceA.GetProperty("currentBalance").GetDecimal());
        Assert.Equal(2222M, balanceB.GetProperty("currentBalance").GetDecimal());

        JsonElement[] accountsA = (await clientA.GetFromJsonAsync<JsonElement[]>(
            "/api/v1/finance/accounts/with-balances"))!;
        Assert.Contains(accountsA, account => account.GetProperty("id").GetGuid() == accountAId);
        Assert.DoesNotContain(accountsA, account => account.GetProperty("id").GetGuid() == accountBId);

        JsonElement[] accountsB = (await clientB.GetFromJsonAsync<JsonElement[]>(
            "/api/v1/finance/accounts/with-balances"))!;
        Assert.Contains(accountsB, account => account.GetProperty("id").GetGuid() == accountBId);
        Assert.DoesNotContain(accountsB, account => account.GetProperty("id").GetGuid() == accountAId);
    }

    [Fact]
    public async Task CrossTenantAccountDebtAndEmployeeIds_Return404()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        HttpClient clientB = await _factory.CreateJwtClientAsync(TenantBAdmin, MultiTenantWebApplicationFactory.TestPassword);

        HttpResponseMessage accountRead = await clientA.GetAsync(
            $"/api/v1/finance/accounts/{_factory.FinancialAccountB.Id}/balance");
        Assert.Equal(HttpStatusCode.NotFound, accountRead.StatusCode);

        using var setBalance = JsonContent.Create(new
        {
            targetBalance = 1M,
            notes = "cross-tenant balance attack",
        });
        HttpResponseMessage accountUpdate = await clientA.PutAsync(
            $"/api/v1/finance/accounts/{_factory.FinancialAccountB.Id}/balance", setBalance);
        Assert.Equal(HttpStatusCode.NotFound, accountUpdate.StatusCode);

        using var createDebt = JsonContent.Create(new
        {
            name = "API B4 Tenant B Debt",
            phone = "0791234567",
            direction = 2,
            currency = "JOD",
            accountId = _factory.FinancialAccountB.Id,
            amount = 250M,
            notes = "cross-tenant debt probe",
            date = DateTime.UtcNow,
        });
        HttpResponseMessage debtCreateResponse = await clientB.PostAsync(
            "/api/v1/finance/debts", createDebt);
        Assert.Equal(HttpStatusCode.Created, debtCreateResponse.StatusCode);
        Guid debtBId = await debtCreateResponse.Content.ReadFromJsonAsync<Guid>();

        JsonElement debtsB = await clientB.GetFromJsonAsync<JsonElement>(
            "/api/v1/finance/debts?search=API%20B4%20Tenant%20B%20Debt");
        Assert.Contains(
            debtsB.GetProperty("items").EnumerateArray(),
            debt => debt.GetProperty("id").GetGuid() == debtBId);

        using var updateDebt = JsonContent.Create(new
        {
            name = "Attempted debt attack",
            phone = (string?)null,
            newAmount = (decimal?)null,
            newAccountId = (Guid?)null,
            notes = "must not be applied",
        });
        HttpResponseMessage debtUpdate = await clientA.PutAsync(
            $"/api/v1/finance/debts/{debtBId}", updateDebt);
        Assert.Equal(HttpStatusCode.NotFound, debtUpdate.StatusCode);

        using var payDebt = JsonContent.Create(new
        {
            accountId = _factory.FinancialAccountA.Id,
            amount = 1M,
            date = DateTime.UtcNow,
            notes = "cross-tenant debt payment attack",
        });
        HttpResponseMessage debtPayment = await clientA.PostAsync(
            $"/api/v1/finance/debts/{debtBId}/payments", payDebt);
        Assert.Equal(HttpStatusCode.NotFound, debtPayment.StatusCode);

        HttpResponseMessage employeeRead = await clientA.GetAsync(
            $"/api/v1/employees/{_factory.EmployeeB.Id}");
        Assert.Equal(HttpStatusCode.NotFound, employeeRead.StatusCode);

        using var updateEmployee = JsonContent.Create(new
        {
            firstName = "Attempted",
            lastName = "Attack",
            role = 4,
            salary = 1000M,
            currency = 1,
            salaryCycle = 3,
            isActive = true,
        });
        HttpResponseMessage employeeUpdate = await clientA.PutAsync(
            $"/api/v1/employees/{_factory.EmployeeB.Id}", updateEmployee);
        Assert.Equal(HttpStatusCode.NotFound, employeeUpdate.StatusCode);

        HttpResponseMessage employeeToggle = await clientA.PostAsync(
            $"/api/v1/employees/{_factory.EmployeeB.Id}/toggle-active", content: null);
        Assert.Equal(HttpStatusCode.NotFound, employeeToggle.StatusCode);

        using var paySalary = JsonContent.Create(new
        {
            accountId = _factory.FinancialAccountA.Id,
            amount = 1M,
            paymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            notes = "cross-tenant salary attack",
        });
        HttpResponseMessage salaryPayment = await clientA.PostAsync(
            $"/api/v1/employees/{_factory.EmployeeB.Id}/pay-salary", paySalary);
        Assert.Equal(HttpStatusCode.NotFound, salaryPayment.StatusCode);
    }

    [Fact]
    public async Task ExpensesAndHrWorkflow_UsesTenantLedgersAndSubresources()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        JsonElement startingBalance = await clientA.GetFromJsonAsync<JsonElement>(
            $"/api/v1/finance/accounts/{_factory.FinancialAccountA.Id}/balance");
        decimal balanceBeforeExpense = startingBalance.GetProperty("currentBalance").GetDecimal();

        using var createCategory = JsonContent.Create(new { name = "API B4 Operations" });
        HttpResponseMessage categoryCreateResponse = await clientA.PostAsync(
            "/api/v1/expenses/categories", createCategory);
        Assert.Equal(HttpStatusCode.Created, categoryCreateResponse.StatusCode);
        Guid categoryId = await categoryCreateResponse.Content.ReadFromJsonAsync<Guid>();

        using var updateCategory = JsonContent.Create(new { name = "API B4 Operations Updated" });
        HttpResponseMessage categoryUpdateResponse = await clientA.PutAsync(
            $"/api/v1/expenses/categories/{categoryId}", updateCategory);
        Assert.Equal(HttpStatusCode.OK, categoryUpdateResponse.StatusCode);

        JsonElement[] categories = (await clientA.GetFromJsonAsync<JsonElement[]>(
            "/api/v1/expenses/categories?activeOnly=false"))!;
        Assert.Contains(categories, category =>
            category.GetProperty("id").GetGuid() == categoryId
            && category.GetProperty("name").GetString() == "API B4 Operations Updated");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        using var createExpense = JsonContent.Create(new
        {
            expenseDate = today,
            categoryId,
            description = "API B4 expense",
            amount = 10M,
            accountId = _factory.FinancialAccountA.Id,
        });
        HttpResponseMessage expenseCreateResponse = await clientA.PostAsync(
            "/api/v1/expenses", createExpense);
        Assert.Equal(HttpStatusCode.Created, expenseCreateResponse.StatusCode);
        Guid expenseId = await expenseCreateResponse.Content.ReadFromJsonAsync<Guid>();

        using var updateExpense = JsonContent.Create(new
        {
            expenseDate = today,
            categoryId,
            description = "API B4 expense updated",
            amount = 12M,
            accountId = _factory.FinancialAccountA.Id,
        });
        HttpResponseMessage expenseUpdateResponse = await clientA.PutAsync(
            $"/api/v1/expenses/{expenseId}", updateExpense);
        Assert.Equal(HttpStatusCode.OK, expenseUpdateResponse.StatusCode);

        JsonElement expenses = await clientA.GetFromJsonAsync<JsonElement>(
            $"/api/v1/expenses?categoryId={categoryId}");
        Assert.Contains(
            expenses.GetProperty("items").EnumerateArray(),
            expense => expense.GetProperty("id").GetGuid() == expenseId
                && expense.GetProperty("amount").GetDecimal() == 12M);

        JsonElement balanceAfterUpdate = await clientA.GetFromJsonAsync<JsonElement>(
            $"/api/v1/finance/accounts/{_factory.FinancialAccountA.Id}/balance");
        Assert.Equal(
            balanceBeforeExpense - 12M,
            balanceAfterUpdate.GetProperty("currentBalance").GetDecimal());

        JsonElement transactions = await clientA.GetFromJsonAsync<JsonElement>(
            "/api/v1/finance/transactions");
        Assert.Contains(
            transactions.GetProperty("items").EnumerateArray(),
            transaction => transaction.GetProperty("referenceType").GetString() == "Expense"
                && transaction.GetProperty("amount").GetDecimal() == 12M);
        Assert.NotEmpty((await clientA.GetFromJsonAsync<JsonElement[]>(
            "/api/v1/finance/transactions/recent?count=5"))!);
        Assert.Equal(HttpStatusCode.OK, (await clientA.GetAsync("/api/v1/expenses/kpis")).StatusCode);

        HttpResponseMessage expenseDeleteResponse = await clientA.DeleteAsync($"/api/v1/expenses/{expenseId}");
        Assert.Equal(HttpStatusCode.OK, expenseDeleteResponse.StatusCode);

        JsonElement balanceAfterDelete = await clientA.GetFromJsonAsync<JsonElement>(
            $"/api/v1/finance/accounts/{_factory.FinancialAccountA.Id}/balance");
        Assert.Equal(
            balanceBeforeExpense,
            balanceAfterDelete.GetProperty("currentBalance").GetDecimal());

        HttpResponseMessage categoryDeleteResponse = await clientA.DeleteAsync(
            $"/api/v1/expenses/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.OK, categoryDeleteResponse.StatusCode);

        using var createEmployee = JsonContent.Create(new
        {
            firstName = "API",
            lastName = "Employee",
            role = 4,
            salary = 1000M,
            currency = 1,
            salaryCycle = 3,
            connectToUser = false,
            existingUserId = (Guid?)null,
            newUserEmail = (string?)null,
            newUserPassword = (string?)null,
        });
        HttpResponseMessage employeeCreateResponse = await clientA.PostAsync(
            "/api/v1/employees", createEmployee);
        Assert.Equal(HttpStatusCode.Created, employeeCreateResponse.StatusCode);
        Guid employeeId = await employeeCreateResponse.Content.ReadFromJsonAsync<Guid>();

        JsonElement employee = await clientA.GetFromJsonAsync<JsonElement>(
            $"/api/v1/employees/{employeeId}");
        Assert.Equal(employeeId, employee.GetProperty("id").GetGuid());

        using var updateEmployee = JsonContent.Create(new
        {
            firstName = "API",
            lastName = "Finance",
            role = 3,
            salary = 1000M,
            currency = 1,
            salaryCycle = 3,
            isActive = true,
        });
        HttpResponseMessage employeeUpdateResponse = await clientA.PutAsync(
            $"/api/v1/employees/{employeeId}", updateEmployee);
        Assert.Equal(HttpStatusCode.OK, employeeUpdateResponse.StatusCode);

        string summaryUrl = $"/api/v1/employees/salary-period-summary?employeeId={employeeId}&paymentDate={today:yyyy-MM-dd}";
        JsonElement summary = await clientA.GetFromJsonAsync<JsonElement>(summaryUrl);
        Assert.Equal(employeeId, summary.GetProperty("employeeId").GetGuid());

        using var paySalary = JsonContent.Create(new
        {
            accountId = _factory.FinancialAccountA.Id,
            amount = 1M,
            paymentDate = today,
            notes = "API B4 salary payment",
        });
        HttpResponseMessage salaryCreateResponse = await clientA.PostAsync(
            $"/api/v1/employees/{employeeId}/pay-salary", paySalary);
        Assert.Equal(HttpStatusCode.Created, salaryCreateResponse.StatusCode);
        Guid salaryPaymentId = await salaryCreateResponse.Content.ReadFromJsonAsync<Guid>();

        JsonElement salaryPayments = await clientA.GetFromJsonAsync<JsonElement>(
            "/api/v1/employees/salary-payments?employeeName=Finance");
        Assert.Contains(
            salaryPayments.GetProperty("items").EnumerateArray(),
            payment => payment.GetProperty("id").GetGuid() == salaryPaymentId);

        JsonElement[] employees = (await clientA.GetFromJsonAsync<JsonElement[]>("/api/v1/employees"))!;
        Assert.Contains(employees, item => item.GetProperty("id").GetGuid() == employeeId);
        Assert.Equal(HttpStatusCode.OK,
            (await clientA.GetAsync("/api/v1/employees/unlinked-users")).StatusCode);

        HttpResponseMessage toggleEmployeeResponse = await clientA.PostAsync(
            $"/api/v1/employees/{employeeId}/toggle-active", content: null);
        Assert.Equal(HttpStatusCode.OK, toggleEmployeeResponse.StatusCode);
    }

    [Fact]
    public async Task FinanceExpensesAndHrFeatureGates_AreIndependent()
    {
        HttpClient noFinanceClient = await _factory.CreateJwtClientAsync(
            MultiTenantWebApplicationFactory.NoFinanceAdmin,
            MultiTenantWebApplicationFactory.TestPassword);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await noFinanceClient.GetAsync("/api/v1/finance/accounts")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await noFinanceClient.GetAsync("/api/v1/finance/debts")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await noFinanceClient.GetAsync("/api/v1/finance/transactions")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await noFinanceClient.GetAsync("/api/v1/expenses/categories")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await noFinanceClient.GetAsync("/api/v1/employees")).StatusCode);

        HttpClient noExpensesClient = await _factory.CreateJwtClientAsync(
            MultiTenantWebApplicationFactory.NoExpensesAdmin,
            MultiTenantWebApplicationFactory.TestPassword);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await noExpensesClient.GetAsync("/api/v1/expenses")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await noExpensesClient.GetAsync("/api/v1/expenses/categories")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await noExpensesClient.GetAsync("/api/v1/finance/accounts")).StatusCode);

        HttpClient noHrClient = await _factory.CreateJwtClientAsync(
            MultiTenantWebApplicationFactory.NoHrAdmin,
            MultiTenantWebApplicationFactory.TestPassword);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await noHrClient.GetAsync("/api/v1/employees")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await noHrClient.GetAsync("/api/v1/employees/salary-payments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await noHrClient.GetAsync("/api/v1/employees/unlinked-users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await noHrClient.GetAsync("/api/v1/finance/accounts")).StatusCode);
    }

    [Fact]
    public async Task StoreOperationsKpis_AreTenantScoped()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        HttpClient clientB = await _factory.CreateJwtClientAsync(TenantBAdmin, MultiTenantWebApplicationFactory.TestPassword);

        decimal jodSalesBeforeA = await GetTodayJodSalesTotalAsync(clientA);
        decimal jodSalesBeforeB = await GetTodayJodSalesTotalAsync(clientB);

        // Deliberately similar data — identical 100 JOD sales in both tenants; each
        // tenant's KPI must reflect only its own increment.
        using (JsonContent saleInA = CreateSalesInvoicePayload(
            _factory.EmployeeA.Id,
            _factory.FinancialAccountA.Id,
            _factory.CategoryA.Id,
            "api-b5-kpi-scoping-a",
            _factory.TenantBId))
        {
            HttpResponseMessage create = await clientA.PostAsync("/api/v1/sales-invoices", saleInA);
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        }

        using (JsonContent saleInB = CreateSalesInvoicePayload(
            _factory.EmployeeB.Id,
            _factory.FinancialAccountB.Id,
            _factory.CategoryB.Id,
            "api-b5-kpi-scoping-b",
            InitialTenant.Id))
        {
            HttpResponseMessage create = await clientB.PostAsync("/api/v1/sales-invoices", saleInB);
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        }

        Assert.Equal(jodSalesBeforeA + 100M, await GetTodayJodSalesTotalAsync(clientA));
        Assert.Equal(jodSalesBeforeB + 100M, await GetTodayJodSalesTotalAsync(clientB));
    }

    [Fact]
    public async Task StoreOperationDetailAndList_AreTenantScoped()
    {
        HttpClient clientA = await _factory.CreateJwtClientAsync(TenantAAdmin, TenantAAdminPassword);
        HttpClient clientB = await _factory.CreateJwtClientAsync(TenantBAdmin, MultiTenantWebApplicationFactory.TestPassword);

        using JsonContent payload = CreateSalesInvoicePayload(
            _factory.EmployeeB.Id,
            _factory.FinancialAccountB.Id,
            _factory.CategoryB.Id,
            "api-b5-detail-attack",
            InitialTenant.Id);
        HttpResponseMessage create = await clientB.PostAsync("/api/v1/sales-invoices", payload);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Guid invoiceId = await create.Content.ReadFromJsonAsync<Guid>();

        HttpResponseMessage ownDetail = await clientB.GetAsync(
            $"/api/v1/dashboard/store-operations/{invoiceId}/detail?operationType=Sale");
        Assert.Equal(HttpStatusCode.OK, ownDetail.StatusCode);
        JsonElement ownBody = await ownDetail.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(invoiceId, ownBody.GetProperty("id").GetGuid());

        // Direct-ID attack: tenant A cannot read tenant B's operation detail.
        HttpResponseMessage crossTenantDetail = await clientA.GetAsync(
            $"/api/v1/dashboard/store-operations/{invoiceId}/detail?operationType=Sale");
        Assert.Equal(HttpStatusCode.NotFound, crossTenantDetail.StatusCode);

        JsonElement listA = await clientA.GetFromJsonAsync<JsonElement>("/api/v1/dashboard/store-operations");
        Assert.DoesNotContain(
            listA.GetProperty("items").EnumerateArray(),
            operation => operation.GetProperty("id").GetGuid() == invoiceId);
    }

    private static async Task<decimal> GetTodayJodSalesTotalAsync(HttpClient client)
    {
        JsonElement kpis = await client.GetFromJsonAsync<JsonElement>(
            "/api/v1/dashboard/store-operations/kpis");

        decimal total = 0M;
        foreach (JsonElement entry in kpis.GetProperty("todaySalesTotals").EnumerateArray())
        {
            // The KPI response uses the display labels (CurrencyLabels), e.g. "Jod".
            if (entry.GetProperty("currency").GetString()?.Equals("JOD", StringComparison.OrdinalIgnoreCase) == true)
            {
                total += entry.GetProperty("amount").GetDecimal();
            }
        }

        return total;
    }

    private static JsonContent CreateSupplierPayload(string name, string primaryPhone, string bankAccountNumber) =>
        JsonContent.Create(new
        {
            name,
            primaryPhone,
            secondaryPhone = (string?)null,
            bankAccountNumber,
            notes = "API isolation test",
        });

    private static JsonContent CreateSalesInvoicePayload(
        Guid employeeId,
        Guid accountId,
        Guid categoryId,
        string notes,
        Guid forgedTenantId) =>
        JsonContent.Create(new
        {
            customerName = "API Customer",
            customerPhone = "0790000000",
            date = DateTime.UtcNow,
            currency = "JOD",
            items = new[]
            {
                new
                {
                    categoryId = (Guid?)categoryId,
                    karat = 21,
                    weightInGrams = 1M,
                    pricePerGram = 100M,
                },
            },
            totalAmount = 100M,
            amountPaid = 100M,
            paymentMethod = 1,
            accountId,
            buyerAccountNumber = (string?)null,
            employeeId,
            paymentLegs = (object?)null,
            notes,
            tenantId = forgedTenantId,
        });

    private static JsonContent CreateCustomerPurchaseInvoicePayload(
        Guid employeeId,
        Guid accountId,
        string notes,
        Guid forgedTenantId) =>
        JsonContent.Create(new
        {
            sellerName = "API Seller",
            sellerPhone = "0791111111",
            sellerIdNumber = "123456789",
            sellerYearOfBirth = 1990,
            sellerAddress = "Amman",
            employeeId,
            currency = "JOD",
            date = DateTime.UtcNow,
            totalAmount = 100M,
            amountPaid = 100M,
            paymentMethod = 1,
            accountId,
            sellerAccountNumber = (string?)null,
            notes,
            items = new[]
            {
                new
                {
                    categoryId = (Guid?)null,
                    karat = 21,
                    weightInGrams = 1M,
                    pricePerGram = 100M,
                },
            },
            paymentLegs = (object?)null,
            tenantId = forgedTenantId,
        });

    private static string IncrementInvoiceNumber(string invoiceNumber)
    {
        int separatorIndex = invoiceNumber.LastIndexOf('-');
        int sequence = int.Parse(invoiceNumber[(separatorIndex + 1)..], System.Globalization.CultureInfo.InvariantCulture);
        return $"{invoiceNumber[..(separatorIndex + 1)]}{sequence + 1:D4}";
    }
}
