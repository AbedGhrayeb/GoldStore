// <copyright file="AccountBalanceResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Finance.Accounts;

public sealed record AccountBalanceResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Currency { get; init; } = string.Empty;

    public string AccountType { get; init; } = string.Empty;

    public decimal CurrentBalance { get; init; }
}
