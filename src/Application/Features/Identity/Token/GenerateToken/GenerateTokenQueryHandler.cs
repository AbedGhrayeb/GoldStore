using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Result;

namespace Application.Features.Identity.Token.GenerateToken;

public sealed class GenerateTokenQueryHandler(ILogger<GenerateTokenQueryHandler> logger,
        IApplicationDbContext dbContext,
        ITokenProvider tokenProvider,
        IPasswordHasher passwordHasher)
    : IQueryHandler<GenerateTokenQuery, TokenResponse>
{
    private readonly ILogger<GenerateTokenQueryHandler> _logger = logger;
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    public async Task<Result<TokenResponse>> Handle(GenerateTokenQuery query, CancellationToken ct)
    {
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == query.Email, ct);

        if (user is null)
        {
            return ApplicationErrors.LoginFailed;
        }

        var verified = _passwordHasher.Verify(query.Password,user.PasswordHash);
        if (!verified)
        {
            return ApplicationErrors.LoginFailed;
        }

        var tokenResult = await _tokenProvider.CreateAsync(user, ct);

        if (tokenResult.IsError)
        {
            _logger.LogError("Generate token error occurred: {ErrorDescription}", tokenResult.Errors[0].Description);
            return ApplicationErrors.TokenGenerationFailed;
        }

        return tokenResult.Value;
    }
}
