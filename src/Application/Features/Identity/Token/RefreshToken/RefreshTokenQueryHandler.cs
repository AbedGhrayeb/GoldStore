using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Errors;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Result;

namespace Application.Features.Identity.Queries.RefreshTokens;

public class RefreshTokenQueryHandler(ILogger<RefreshTokenQueryHandler> logger, IApplicationDbContext dbContext, ITokenProvider tokenProvider, IUserContext userContext)
    : IQueryHandler<RefreshTokenQuery, TokenResponse>
{
    private readonly ILogger<RefreshTokenQueryHandler> _logger = logger;
    private readonly IApplicationDbContext _context = dbContext;
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    private readonly IUserContext _userContext = userContext;

    public async Task<Result<TokenResponse>> Handle(RefreshTokenQuery request, CancellationToken ct)
    {
        var principal = _tokenProvider.GetPrincipalFromExpiredToken(request.ExpiredAccessToken);

        if (principal is null)
        {
            _logger.LogError("Expired access token is not valid");

            return ApplicationErrors.ExpiredAccessTokenInvalid;
        }

        var userId = _userContext.UserId;

        if (userId == Guid.Empty)
        {
            _logger.LogError("Invalid userId claim");

            return UserErrors.IdRequired;
        }

        var getUserResult = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (getUserResult is null)
        {
            _logger.LogError("User not found");

            return UserErrors.NotFound(userId);
        }

        var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == request.RefreshToken && r.UserId == userId, ct);

        if (refreshToken is null || refreshToken.ExpiresOnUtc < DateTime.UtcNow)
        {
            _logger.LogError("Refresh token has expired");

            return ApplicationErrors.RefreshTokenExpired;
        }

        var generateTokenResult = await _tokenProvider.CreateAsync(getUserResult, ct);

        if (generateTokenResult.IsError)
        {
            _logger.LogError("Generate token error occurred: {ErrorDescription}", generateTokenResult.TopError.Description);

            return generateTokenResult.Errors;
        }

        return generateTokenResult.Value;
    }

}
