using Application.Abstractions.Messaging;
using Application.Common.Models;

namespace Application.Employees.GetSalaryPayments;

public sealed record GetSalaryPaymentsQuery(
    int Page = 1,
    int PageSize = 20,
    string? EmployeeName = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IQuery<PaginatedList<SalaryPaymentResponse>>;
