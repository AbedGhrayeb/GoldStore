// <copyright file="IRefreshTokenService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<RefreshTokenResponse> IssueAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Validates and rotates a refresh token, returning a new one-token pair.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<RefreshTokenResponse>> RotateAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>Revokes the given refresh token (idempotent).</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>Revokes every active refresh token issued to the user (account disable, stamp rotation).</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);
}
