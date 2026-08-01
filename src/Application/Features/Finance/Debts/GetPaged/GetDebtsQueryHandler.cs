using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Models;
using Domain.Debts;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Application.Features.Finance.Debts.GetPaged;

internal sealed class GetDebtsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetDebtsQuery, PaginatedList<DebtResponse>>
{
    private static string GetDirectionLabel(DebtDirection direction) => direction switch
    {
        DebtDirection.Receivable => "ذمة مدينة",
        DebtDirection.Payable => "ذمة دائنة",
        _ => direction.ToString()
    };

    public async Task<Result<PaginatedList<DebtResponse>>> Handle(GetDebtsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Debt> debtsQuery = context.Debts.OrderByDescending(d=>d.CreatedAtUtc).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Direction) &&
            Enum.TryParse<DebtDirection>(query.Direction, out DebtDirection direction))
        {
            debtsQuery = debtsQuery.Where(d => d.Direction == direction);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search.ToLower().Trim();
            debtsQuery = debtsQuery.Where(d =>
                d.Name.ToLower().Contains(search) ||
                d.Phone != null && d.Phone.Contains(search));
        }


        var debtIds = debtsQuery.Select(d => d.Id).ToList();

        List<DebtLedgerEntry> allEntries = await context.DebtLedgerEntries
            .AsNoTracking()
            .Where(e => debtIds.Contains(e.DebtId))
            .ToListAsync(cancellationToken);

        var balances = allEntries
            .GroupBy(e => e.DebtId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(e => e.MovementType == DebtBalanceMovementType.Increase ? e.Amount : -e.Amount));

        IQueryable<DebtResponse> items = debtsQuery.Select(d => new DebtResponse
        {
            Id = d.Id,
            Name = d.Name,
            Phone = d.Phone,
            Direction = d.Direction.ToString(),
            DirectionLabel = GetDirectionLabel(d.Direction),
            Currency = d.Currency.ToString(),
            AccountId = d.AccountId,
            Notes = d.Notes,
            CreatedAt = d.CreatedAtUtc!.Value.LocalDateTime,
            OutstandingBalance = balances.GetValueOrDefault(d.Id, 0m),
            OutstandingBalanceDisplay = balances.GetValueOrDefault(d.Id, 0m).ToString("N3")
        });

        return await PaginatedList<DebtResponse>.CreateAsync(items, query.Page, query.PageSize);

        ;
    }
}
