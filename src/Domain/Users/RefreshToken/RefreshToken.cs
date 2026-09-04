using SharedKernel;
using SharedKernel.Result;

namespace Domain.Users.RefreshToken;

public sealed class RefreshToken : Entity
{
    public string? Token { get; }
    public Guid? UserId { get; }
    public DateTimeOffset ExpiresOnUtc { get; }

    private RefreshToken()
    { }

    private RefreshToken(Guid id, string? token, Guid? userId, DateTimeOffset expiresOnUtc)
        : base(id)
    {
        Token = token;
        UserId = userId;
        ExpiresOnUtc = expiresOnUtc;
    }

    public static Result<RefreshToken> Create(Guid id, string? token, Guid? userId, DateTimeOffset expiresOnUtc)
    {
        if (id == Guid.Empty)
        {
            return RefreshTokenErrors.IdRequired;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return RefreshTokenErrors.TokenRequired;
        }

        if (userId == Guid.Empty || userId == null)
        {
            return RefreshTokenErrors.UserIdRequired;
        }

        if (expiresOnUtc <= DateTimeOffset.UtcNow)
        {
            return RefreshTokenErrors.ExpiryInvalid;
        }

        return new RefreshToken(id, token, userId, expiresOnUtc);
    }
}
