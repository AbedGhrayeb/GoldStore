using SharedKernel;

namespace Domain.Users;

public sealed class UserRecoveryCode : Entity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public string CodeHash { get; private set; }

    public bool IsUsed { get; private set; }

    public DateTimeOffset? UsedAtUtc { get; private set; }

    private UserRecoveryCode() { CodeHash = string.Empty; }

    private UserRecoveryCode(Guid id, Guid tenantId, Guid userId, string codeHash) : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        CodeHash = codeHash;
    }

    public static UserRecoveryCode Create(Guid tenantId, Guid userId, string codeHash)
        => new(Guid.CreateVersion7(), tenantId, userId, codeHash);

    public void MarkUsed(DateTimeOffset utcNow)
    {
        IsUsed = true;
        UsedAtUtc = utcNow;
    }
}
