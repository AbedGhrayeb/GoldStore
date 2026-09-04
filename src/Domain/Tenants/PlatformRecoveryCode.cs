using SharedKernel;

namespace Domain.Tenants;

public sealed class PlatformRecoveryCode : Entity
{
    public Guid PlatformUserId { get; private set; }

    public string CodeHash { get; private set; }

    public bool IsUsed { get; private set; }

    public DateTimeOffset? UsedAtUtc { get; private set; }

    private PlatformRecoveryCode() { CodeHash = string.Empty; }

    private PlatformRecoveryCode(Guid id, Guid platformUserId, string codeHash) : base(id)
    {
        PlatformUserId = platformUserId;
        CodeHash = codeHash;
    }

    public static PlatformRecoveryCode Create(Guid platformUserId, string codeHash)
        => new(Guid.CreateVersion7(), platformUserId, codeHash);

    public void MarkUsed(DateTimeOffset utcNow)
    {
        IsUsed = true;
        UsedAtUtc = utcNow;
    }
}
