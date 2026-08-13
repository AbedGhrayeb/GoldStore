using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Tenants;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Finance.Accounts.GetAll;

internal sealed class GetFinancialAccountsQueryHandler(IApplicationDbContext context, ICurrentTenant currentTenant)
    : IQueryHandler<GetFinancialAccountsQuery, List<FinancialAccountResponse>>
{
    public async Task<Result<List<FinancialAccountResponse>>> Handle(GetFinancialAccountsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<FinancialAccount> accounts = context.FinancialAccounts.AsNoTracking().Where(a => a.TenantId == currentTenant.TenantId);

        if (query.ActiveOnly)
        {
            accounts = accounts.Where(a => a.IsActive);
        }

        List<FinancialAccountResponse> result = await accounts
            .OrderByDescending(a => a)
            .Select(a => new FinancialAccountResponse
            {
                Id = a.Id,
                Name = a.Name,
                Currency = a.Currency.ToString(),
                AccountType = a.AccountType.ToString(),
                AccountNumber = a.AccountNumber,
                IsActive = a.IsActive
            })
            .ToListAsync(cancellationToken);

        return result;
    }
}
