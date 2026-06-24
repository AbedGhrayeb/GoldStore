using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Finance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Finance.Accounts.GetAll;

internal sealed class GetFinancialAccountsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetFinancialAccountsQuery, List<FinancialAccountResponse>>
{
    public async Task<Result<List<FinancialAccountResponse>>> Handle(GetFinancialAccountsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<FinancialAccount> accounts = context.FinancialAccounts.AsNoTracking();

        if (query.ActiveOnly)
        {
            accounts = accounts.Where(a => a.IsActive);
        }

        List<FinancialAccountResponse> result = await accounts
            .OrderBy(a => a.Name)
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