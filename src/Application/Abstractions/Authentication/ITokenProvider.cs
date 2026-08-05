using System.Security.Claims;
using Application.Features.Identity;
using Domain.Users;
using SharedKernel.Result;

namespace Application.Abstractions.Authentication;

public interface ITokenProvider
{
    Task<Result<TokenResponse>> CreateAsync(User user,CancellationToken ct);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
