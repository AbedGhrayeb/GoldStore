namespace Application.Abstractions.Phone;

public sealed record VerifiedPhone(string E164);

public interface IPhoneVerifier
{
    Task<VerifiedPhone> VerifyAsync(string idToken, CancellationToken cancellationToken);
}
