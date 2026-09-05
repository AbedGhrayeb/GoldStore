// <copyright file="TwoFactorOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Infrastructure.Phone;

public sealed class TwoFactorOptions
{
    public const string SectionName = "TwoFactor";

    public bool Mandatory { get; set; } = true;

    public int TempTicketMinutes { get; set; } = 5;

    public int RecoveryCodeCount { get; set; } = 8;
}
