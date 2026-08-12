using SharedKernel;
using SharedKernel.Result;

namespace Domain.Users;

/// <summary>
/// A rotating refresh token (plan Phase 4 item 5). Tenant-owned so the token stays
/// bound to the tenant that issued it; lookups are keyed by token hash because the
/// plain token is only ever held by the client.
/// </summary>
public sealed class RefreshToken : Entity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>SHA-256 hash of the opaque token. The raw token is never stored.</summary>
    public string TokenHash { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    /// <summary>Hash of the token that replaced this one (rotation chain, plan Phase 4).</summary>
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>True when the token has not been revoked or replaced (expiry is checked separately).</summary>
    public bool IsUsable => RevokedAtUtc is null && ReplacedByTokenHash is null;

    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    private RefreshToken(Guid id, Guid tenantId, Guid userId, string tokenHash, DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public static Result<RefreshToken> Create(
        Guid tenantId,
        Guid userId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (tenantId == Guid.Empty)
        {
            return UserErrors.TenantRequired;
        }

        if (userId == Guid.Empty)
        {
            return UserErrors.IdRequired;
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return UserErrors.RefreshTokenRequired;
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            return UserErrors.InvalidRefreshTokenExpiry;
        }

        return new RefreshToken(Guid.CreateVersion7(), tenantId, userId, tokenHash, createdAtUtc, expiresAtUtc);
    }

    /// <summary>Revokes this token, optionally recording the replacement token hash.</summary>
    public void Revoke(DateTimeOffset revokedAtUtc, string? replacedByTokenHash = null)
    {
        RevokedAtUtc = revokedAtUtc;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
