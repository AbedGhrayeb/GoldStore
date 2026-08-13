using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Finance;
using Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Result;

namespace Application.Features.SupplierFinancialTransactions.GetPayments;

internal sealed class GetPaymentsByTransactionIdQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetPaymentsByTransactionIdQuery, List<SupplierFinancialPaymentResponse>>
{
    public async Task<Result<List<SupplierFinancialPaymentResponse>>> Handle(
        GetPaymentsByTransactionIdQuery query,
        CancellationToken cancellationToken)
    {
        bool exists = await context.SupplierFinancialTransactions.AnyAsync(
            t => t.TenantId == currentTenant.TenantId && t.Id == query.TransactionId, cancellationToken);

        if (!exists)
        {
            return SupplierFinancialErrors.NotFound(query.TransactionId);
        }

        List<SupplierFinancialPaymentResponse> payments = await context.SupplierFinancialPayments
            .Where(p => p.TenantId == currentTenant.TenantId)
            .Where(p => p.SupplierFinancialTransactionId == query.TransactionId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new SupplierFinancialPaymentResponse
            {
                Id = p.Id,
                Amount = p.Amount,
                AccountName = context.FinancialAccounts
                    .Where(a => a.TenantId == currentTenant.TenantId)
                    .Where(a => a.Id == p.AccountId)
                    .Select(a => a.Name)
                    .FirstOrDefault() ?? string.Empty,
                Date = p.CreatedAtUtc!.Value.LocalDateTime,
                Notes = p.Notes
            })
            .ToListAsync(cancellationToken);

        return payments;
    }
}
