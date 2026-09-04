using Application.Abstractions.Phone;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Options;

namespace Infrastructure.Phone;

internal sealed class FirebasePhoneVerifier(IOptions<FirebaseOptions> options) : IPhoneVerifier
{
    public async Task<VerifiedPhone> VerifyAsync(string idToken, CancellationToken cancellationToken)
    {
        if (FirebaseApp.DefaultInstance is null)
        {
            throw new InvalidOperationException("FirebaseApp not initialized. Configure Firebase:ProjectId / ServiceAccount.");
        }

        FirebaseToken decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, cancellationToken);

        string? phone = decoded.Claims.TryGetValue("phone_number", out object? pn) ? pn?.ToString() : null;
        phone ??= decoded.Claims.TryGetValue("phoneNumber", out object? pn2) ? pn2?.ToString() : null;

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new InvalidOperationException("Firebase idToken does not contain phone_number.");
        }

        if (!IsValidE164(phone))
        {
            throw new InvalidOperationException($"Firebase phone_number '{phone}' is not valid E164.");
        }

        // Optional: enforce provider == phone / aud == project
        if (decoded.Claims.TryGetValue("firebase", out object? fbObj) && fbObj is IDictionary<string, object> fb)
        {
            if (fb.TryGetValue("sign_in_provider", out object? provider) && provider?.ToString() != "phone")
            {
                throw new InvalidOperationException($"Unexpected sign_in_provider '{provider}'. Expected 'phone'.");
            }
        }

        string? projectId = options.Value.ProjectId;
        if (!string.IsNullOrWhiteSpace(projectId) && decoded.Audience != projectId)
        {
            // Firebase Admin already validates aud, this is extra defense
            throw new InvalidOperationException($"Audience mismatch '{decoded.Audience}' != '{projectId}'.");
        }

        return new VerifiedPhone(phone);
    }

    private static bool IsValidE164(string e164) =>
        e164.StartsWith('+') && e164.Length is >= 8 and <= 16 && e164[1..].All(char.IsDigit);
}
