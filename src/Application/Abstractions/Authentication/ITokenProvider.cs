// <copyright file="ITokenProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<AccessTokenResponse> CreateAccessTokenAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>A short-lived access token for a platform administrator.</summary>
public sealed record PlatformAccessTokenResponse(string Value, DateTimeOffset ExpiresAtUtc);

/// <summary>
/// Creates JWTs for host administrators. Platform identities deliberately carry no tenant
/// claims and can therefore authenticate only on host-only endpoints.
/// </summary>
public interface IPlatformTokenProvider
{
    Task<PlatformAccessTokenResponse> CreateAccessTokenAsync(Guid platformUserId, CancellationToken cancellationToken);
}
