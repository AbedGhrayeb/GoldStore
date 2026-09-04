using Application.Abstractions.Phone;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Phone;

/// <summary>
/// Development-only verifier. Accepts idToken == "dev-token:+970XXXXXXXXX" and returns that E164.
/// Never registered in Production.
/// </summary>
internal sealed class LogPhoneVerifier(IHostEnvironment env, IConfiguration configuration) : IPhoneVerifier
{
    public Task<VerifiedPhone> VerifyAsync(string idToken, CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment() && configuration.GetValue("Firebase:AllowDevTokens", false) is false)
        {
            throw new InvalidOperationException("LogPhoneVerifier is only available in Development or when Firebase:AllowDevTokens=true. Use FirebasePhoneVerifier.");
        }

        const string prefix = "dev-token:";
        if (!idToken.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid dev-token format. Expected 'dev-token:+970...' .");
        }

        string e164 = idToken[prefix.Length..].Trim();
        if (!IsValidE164(e164))
        {
            throw new InvalidOperationException($"Invalid E164 '{e164}'. Expected '+970...' .");
        }

        return Task.FromResult(new VerifiedPhone(e164));
    }

    private static bool IsValidE164(string e164) =>
        e164.StartsWith('+') && e164.Length is >= 8 and <= 16 && e164[1..].All(char.IsDigit);
}
