// <copyright file="EmployeeResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Common;
using Domain.Employees;

namespace Application.Employees;

public sealed record EmployeeResponse
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public RoleEnum Role { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public decimal Salary { get; set; }

    public Currency Currency { get; set; }

    public string CurrencySymbol { get; set; } = string.Empty;

    public SalaryCycleEnum SalaryCycle { get; set; }

    public string SalaryCycleName { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public string? UserEmail { get; set; }

    public DateOnly? LastPaymentDate { get; set; }

    public decimal? LastPaymentNet { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
