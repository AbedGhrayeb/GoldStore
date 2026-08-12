namespace Application.Abstractions.Authentication;

/// <summary>A short-lived access token (JWT) bound to a user and one tenant.</summary>
public sealed record AccessTokenResponse(string Value, DateTimeOffset ExpiresAtUtc);

/// <summary>A rotated refresh token, still bound to the user and tenant it was issued for.</summary>
public sealed record RefreshTokenResponse(Guid UserId, string Value, DateTimeOffset ExpiresAtUtc);

public interface ITokenProvider
{
    /// <summary>
    /// Creates an access token carrying immutable claims: user id, tenant id and key,
    /// roles, permissions, and the user's current security stamp (plan Phase 4 items 3-4).
    /// </summary>
    Task<AccessTokenResponse> CreateAccessTokenAsync(Guid userId, CancellationToken cancellationToken);
}
