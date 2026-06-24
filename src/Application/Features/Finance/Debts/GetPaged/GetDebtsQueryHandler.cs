using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Debts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Finance.Debts.GetPaged;

internal sealed class GetDebtsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetDebtsQuery, PagedDebtResponse>
{
    private static string GetDirectionLabel(DebtDirection direction) => direction switch
    {
        DebtDirection.Receivable => "ذمة مدينة",
        DebtDirection.Payable => "ذمة دائنة",
        _ => direction.ToString()
    };

    public async Task<Result<PagedDebtResponse>> Handle(GetDebtsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Debt> debtsQuery = context.Debts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Direction) &&
            Enum.TryParse<DebtDirection>(query.Direction, out var direction))
        {
            debtsQuery = debtsQuery.Where(d => d.Direction == direction);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search.Trim();
            debtsQuery = debtsQuery.Where(d =>
                d.Name.Contains(search) ||
                (d.Phone != null && d.Phone.Contains(search)));
        }

        int totalCount = await debtsQuery.CountAsync(cancellationToken);

        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        List<Debt> pagedData = await debtsQuery
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        List<Guid> debtIds = pagedData.Select(d => d.Id).ToList();

        List<DebtLedgerEntry> allEntries = await context.DebtLedgerEntries
            .AsNoTracking()
            .Where(e => debtIds.Contains(e.DebtId))
            .ToListAsync(cancellationToken);

        var balances = allEntries
            .GroupBy(e => e.DebtId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(e => e.MovementType == DebtBalanceMovementType.Increase ? e.Amount : -e.Amount));

        List<DebtResponse> items = pagedData.Select(d => new DebtResponse
        {
            Id = d.Id,
            Name = d.Name,
            Phone = d.Phone,
            Direction = d.Direction.ToString(),
            DirectionLabel = GetDirectionLabel(d.Direction),
            Currency = d.Currency.ToString(),
            AccountId = d.AccountId,
            Notes = d.Notes,
            CreatedAt = d.CreatedAt,
            OutstandingBalance = balances.GetValueOrDefault(d.Id, 0m),
            OutstandingBalanceDisplay = balances.GetValueOrDefault(d.Id, 0m).ToString("N3")
        }).ToList();

        return new PagedDebtResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
