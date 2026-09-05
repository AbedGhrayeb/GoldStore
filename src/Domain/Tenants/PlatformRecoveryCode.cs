// <copyright file="PlatformRecoveryCode.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel;

namespace Domain.Tenants;

public sealed class PlatformRecoveryCode : Entity
{
    public Guid PlatformUserId { get; private set; }

    public string CodeHash { get; private set; }

    public bool IsUsed { get; private set; }

    public DateTimeOffset? UsedAtUtc { get; private set; }

    private PlatformRecoveryCode()
    {
        this.CodeHash = string.Empty;
    }

    private PlatformRecoveryCode(Guid id, Guid platformUserId, string codeHash)
        : base(id)
    {
        this.PlatformUserId = platformUserId;
        this.CodeHash = codeHash;
    }

    public static PlatformRecoveryCode Create(Guid platformUserId, string codeHash)
        => new(Guid.CreateVersion7(), platformUserId, codeHash);

    public void MarkUsed(DateTimeOffset utcNow)
    {
        this.IsUsed = true;
        this.UsedAtUtc = utcNow;
    }
}
