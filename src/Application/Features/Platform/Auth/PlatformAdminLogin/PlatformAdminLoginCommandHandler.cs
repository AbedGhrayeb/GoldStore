using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common.Errors;
using Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Result;

namespace Application.Features.Platform.Auth.PlatformAdminLogin;

internal sealed class PlatformAdminLoginCommandHandler(
    ILogger<PlatformAdminLoginCommandHandler> logger,
    IPlatformDbContext dbContext,
    IPasswordHasher passwordHasher,
    IPlatformTokenProvider tokenProvider)
    : ICommandHandler<PlatformAdminLoginCommand, PlatformAdminLoginResponse>
{
    public async Task<Result<PlatformAdminLoginResponse>> Handle(PlatformAdminLoginCommand command, CancellationToken cancellationToken)
    {
        PlatformAdmin? platformAdmin = await dbContext.PlatformAdmins
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Email == command.Email, cancellationToken);

        if (platformAdmin is null || !passwordHasher.Verify(command.Password, platformAdmin.PasswordHash))
        {
            return PlatformAdminErrors.LoginFailed;
        }

        Result<PlatformAdminLoginResponse> tokenResult = await tokenProvider.CreateAsync(platformAdmin, cancellationToken);

        if (tokenResult.IsError)
        {
            logger.LogError("Platform admin token generation failed: {ErrorDescription}", tokenResult.Errors[0].Description);
            return ApplicationErrors.TokenGenerationFailed;
        }

        return tokenResult.Value;
    }
}
