using Application.Abstractions.Messaging;

namespace Application.Features.SupplierFinancialTransactions.GetKpis;

public sealed record GetSupplierFinancialKpisQuery : IQuery<SupplierFinancialKpiResponse>;
