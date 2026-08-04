using Application.Abstractions.Messaging;

namespace Application.Features.Identity.Token.GenerateToken;

public sealed record GenerateTokenQuery(string Email, string Password) : IQuery<TokenResponse>
{
}
