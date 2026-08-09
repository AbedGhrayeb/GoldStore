using Application.Features.Platform.Auth.PlatformAdminLogin;
using Domain.Tenants;
using SharedKernel.Result;

namespace Application.Abstractions.Authentication;

public interface IPlatformTokenProvider
{
    Task<Result<PlatformAdminLoginResponse>> CreateAsync(PlatformAdmin platformAdmin, CancellationToken ct);
}
