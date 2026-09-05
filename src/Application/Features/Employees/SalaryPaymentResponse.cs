// <copyright file="SalaryPaymentResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Employees;

public sealed record SalaryPaymentResponse
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public DateOnly PaymentDate { get; set; }

    public DateOnly ScheduledDate { get; set; }

    public decimal SalaryAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal Amount { get; set; }

    public string? AccountName { get; set; }

    public string? Notes { get; set; }

    public bool IsOnSchedule { get; set; }
}
