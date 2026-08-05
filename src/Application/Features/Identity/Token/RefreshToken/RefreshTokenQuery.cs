using Application.Abstractions.Messaging;

namespace Application.Features.Identity.Queries.RefreshTokens;

public record RefreshTokenQuery(string RefreshToken, string ExpiredAccessToken) : IQuery<TokenResponse>;
