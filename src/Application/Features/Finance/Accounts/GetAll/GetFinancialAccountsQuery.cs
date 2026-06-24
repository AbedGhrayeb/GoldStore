using Application.Abstractions.Messaging;

namespace Application.Finance.Accounts.GetAll;

public sealed record GetFinancialAccountsQuery(bool ActiveOnly = true) : IQuery<List<FinancialAccountResponse>>;