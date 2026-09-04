using Application.Abstractions.Caching;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.CustomerPurchases;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Result;

namespace Application.Features.CustomerPurchaseInvoices.GetKpis;

internal sealed class GetCustomerPurchaseInvoicesKpisQueryHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    ICurrentTenant currentTenant,
    ICacheService cache)
    : IQueryHandler<GetCustomerPurchaseInvoicesKpisQuery, CustomerPurchaseInvoiceKpiResponse>
{
    public async Task<Result<CustomerPurchaseInvoiceKpiResponse>> Handle(
        GetCustomerPurchaseInvoicesKpisQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsAvailable)
        {
            return await BuildAsync(cancellationToken);
        }

        Guid tenantId = currentTenant.TenantId;
        if (currentTenant is ICurrentTenantSetter setter)
        {
            setter.Set(tenantId, currentTenant.TenantKey);
        }

        string cacheKey = CacheKeys.Kpi(tenantId, "customer-purchases");
        CustomerPurchaseInvoiceKpiResponse response = await cache.GetOrCreateAsync(
            cacheKey,
            [CacheKeys.KpiTenant(tenantId)],
            (ct) => BuildAsync(ct),
            CacheKeys.KpiExpiration,
            cancellationToken);

        return response;
    }

    private async Task<CustomerPurchaseInvoiceKpiResponse> BuildAsync(CancellationToken cancellationToken)
    {
        DateTime todayStart = dateTimeProvider.UtcNow.Date;

        List<CustomerPurchaseInvoice> todayInvoices = await context.CustomerPurchaseInvoices
            .AsNoTracking()
            .Where(i => i.Date >= todayStart)
            .ToListAsync(cancellationToken);

        int count = todayInvoices.Count;
        decimal totalPurchases = todayInvoices.Sum(i => i.TotalAmount);
        decimal totalPaid = todayInvoices.Sum(i => i.AmountPaid);
        decimal totalRemaining = todayInvoices.Sum(i => i.RemainingBalance);

        return new CustomerPurchaseInvoiceKpiResponse
        {
            TodayCount = count,
            TotalPurchases = totalPurchases,
            TotalPurchasesDisplay = totalPurchases.ToString("N3"),
            TotalPaid = totalPaid,
            TotalPaidDisplay = totalPaid.ToString("N3"),
            TotalRemaining = totalRemaining,
            TotalRemainingDisplay = totalRemaining.ToString("N3")
        };
    }
}
