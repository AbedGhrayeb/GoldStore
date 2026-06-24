using Application.Abstractions.Messaging;

namespace Application.Features.Finance.Debts.GetKpis;

public sealed record GetDebtKpisQuery : IQuery<DebtKpiResponse>;
