// <copyright file="GetSalaryPeriodSummaryQuery.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Abstractions.Messaging;

namespace Application.Employees.GetSalaryPeriodSummary;

public sealed record GetSalaryPeriodSummaryQuery(
    Guid EmployeeId,
    DateOnly PaymentDate) : IQuery<SalaryPeriodSummaryResponse>;
