using SharedKernel.Result;

namespace Application.Abstractions.Authentication;

/// <summary>
/// Issuance, rotation, and revocation of refresh tokens (plan Phase 4 item 5). Tokens
/// are stored hashed, bound to a tenant, and revoked when the user or tenant is no
/// longer eligible.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>Issues a new refresh token for the given user.</summary>
    Task<RefreshTokenResponse> IssueAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Validates and rotates a refresh token, returning a new one-token pair.</summary>
    Task<Result<RefreshTokenResponse>> RotateAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>Revokes the given refresh token (idempotent).</summary>
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>Revokes every active refresh token issued to the user (account disable, stamp rotation).</summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);
}
