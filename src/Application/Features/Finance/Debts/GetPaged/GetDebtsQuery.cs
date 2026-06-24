using Application.Abstractions.Messaging;

namespace Application.Features.Finance.Debts.GetPaged;

public sealed record GetDebtsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Direction = null,
    string? Search = null) : IQuery<PagedDebtResponse>;
