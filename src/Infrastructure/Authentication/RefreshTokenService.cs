using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Tenants;
using Application.Common.Errors;
using Domain.Tenants;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Result;

namespace Infrastructure.Authentication;

/// <summary>
/// Issues, rotates, and revokes refresh tokens (plan Phase 4 item 5). Tokens are stored
/// hashed, bound to a tenant and user, and rotated on every use. Tenant context does not
/// exist during login or when a token is presented for refresh, so lookups bypass the
/// tenant query filter and the ambient tenant is selected explicitly before any write —
/// the same host/background-flow pattern the seeder uses.
/// </summary>
internal sealed class RefreshTokenService(
    IApplicationDbContext context,
    ICurrentTenantSetter currentTenantSetter,
    IConfiguration configuration,
    TimeProvider timeProvider) : IRefreshTokenService
{
    public async Task<RefreshTokenResponse> IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        User user = await LoadUserAsync(userId, cancellationToken);
        Tenant tenant = await LoadTenantAsync(user.TenantId, cancellationToken);

        // Login runs before any tenant context exists; select the tenant explicitly so the
        // write guard can stamp the new token row.
        currentTenantSetter.Set(user.TenantId, tenant.Key);

        (string plainToken, RefreshToken entity) = CreateToken(user.TenantId, user.Id);

        context.RefreshTokens.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse(user.Id, plainToken, entity.ExpiresAtUtc);
    }

    public async Task<Result<RefreshTokenResponse>> RotateAsync(string refreshToken, CancellationToken cancellationToken)
    {
        string tokenHash = Hash(refreshToken);

        RefreshToken? stored = await context.RefreshTokens
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        DateTimeOffset utcNow = timeProvider.GetUtcNow();

        if (stored is null || !stored.IsUsable || stored.ExpiresAtUtc <= utcNow)
        {
            return ApplicationErrors.InvalidRefreshToken;
        }

        User user = await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == stored.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return ApplicationErrors.UserDisabled;
        }

        Tenant tenant = await context.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == stored.TenantId, cancellationToken);

        if (tenant is null || !IsSignInAllowed(tenant, utcNow))
        {
            return ApplicationErrors.TenantAccessDenied;
        }

        currentTenantSetter.Set(stored.TenantId, tenant.Key);

        (string plainToken, RefreshToken replacement) = CreateToken(stored.TenantId, stored.UserId);
        stored.Revoke(utcNow, replacement.TokenHash);

        context.RefreshTokens.Add(replacement);
        await context.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse(stored.UserId, plainToken, replacement.ExpiresAtUtc);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        string tokenHash = Hash(refreshToken);

        RefreshToken? stored = await context.RefreshTokens
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (stored is null || !stored.IsUsable)
        {
            return;
        }

        await SelectTenantAsync(stored.TenantId, cancellationToken);
        stored.Revoke(timeProvider.GetUtcNow());
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        List<RefreshToken> activeTokens = await context.RefreshTokens
            .IgnoreQueryFilters()
            .Where(token => token.UserId == userId && token.IsUsable)
            .ToListAsync(cancellationToken);

        if (activeTokens.Count == 0)
        {
            return;
        }

        User user = await LoadUserAsync(userId, cancellationToken);
        await SelectTenantAsync(user.TenantId, cancellationToken);

        DateTimeOffset utcNow = timeProvider.GetUtcNow();
        foreach (RefreshToken token in activeTokens)
        {
            token.Revoke(utcNow);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private (string Token, RefreshToken Entity) CreateToken(Guid tenantId, Guid userId)
    {
        string token = GenerateToken();
        DateTimeOffset utcNow = timeProvider.GetUtcNow();
        DateTimeOffset expiresAt = utcNow.AddDays(configuration.GetValue("Jwt:RefreshExpirationInDays", 14));

        RefreshToken entity = RefreshToken
            .Create(tenantId, userId, Hash(token), utcNow, expiresAt)
            .Match(
                value => value,
                _ => throw new InvalidOperationException("Failed to create a refresh token."));

        return (token, entity);
    }

    private async Task SelectTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        Tenant tenant = await LoadTenantAsync(tenantId, cancellationToken);
        currentTenantSetter.Set(tenantId, tenant.Key);
    }

    private async Task<User> LoadUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
        ?? throw new InvalidOperationException($"Cannot issue a token for unknown user '{userId}'.");

    private async Task<Tenant> LoadTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await context.Tenants
            .AsNoTracking()
            .SingleAsync(t => t.Id == tenantId, cancellationToken);

    private static bool IsSignInAllowed(Tenant tenant, DateTimeOffset utcNow) => tenant.Status switch
    {
        TenantStatus.Active => true,
        TenantStatus.Trial => tenant.TrialEndsAtUtc is null || tenant.TrialEndsAtUtc > utcNow,
        TenantStatus.Cancelled => tenant.CancellationReadOnlyUntilUtc is not null && tenant.CancellationReadOnlyUntilUtc > utcNow,
        _ => false,
    };

    private static string GenerateToken() => RandomNumberGenerator.GetHexString(64, lowercase: true);

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
