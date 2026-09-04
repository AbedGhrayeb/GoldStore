using System.Security.Cryptography;
using SharedKernel;
using SharedKernel.Result;

namespace Domain.Users;

public sealed class User : Entity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public string Email { get; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string PasswordHash { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? WhatsappNumber { get; private set; }

    public bool TwoFactorEnabled { get; private set; }

    public bool PhoneNumberVerified { get; private set; }

    public DateTimeOffset? TwoFactorEnabledAtUtc { get; private set; }

    /// <summary>
    /// Session/token version (plan Phase 4). Every issued cookie, access token, and
    /// refresh token carries this value; changing it invalidates all previously
    /// issued sessions. Regenerate on password change and on account disable.
    /// </summary>
    public string SecurityStamp { get; private set; }

    /// <summary>Consecutive failed sign-in attempts since the last success (M7 lockout).</summary>
    public int FailedLoginAttempts { get; private set; }

    /// <summary>Instant until which the account is locked out (M7 lockout). Null = not locked.</summary>
    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public const int MaxFailedLoginAttempts = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public User()
    {
        Email = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        PasswordHash = string.Empty;
        SecurityStamp = string.Empty;
    }
    public User(Guid id, Guid tenantId, string email, string firstName, string lastName, string passwordHash, string? phoneNumber = null, string? whatsappNumber = null) : base(id)
    {
        TenantId = tenantId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PasswordHash = passwordHash;
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        WhatsappNumber = string.IsNullOrWhiteSpace(whatsappNumber) ? null : whatsappNumber.Trim();
        SecurityStamp = NewSecurityStamp();

    }

    public bool IsLockedOut(DateTimeOffset utcNow) => LockedUntilUtc is not null && LockedUntilUtc > utcNow;

    /// <summary>
    /// Records a failed sign-in. After <paramref name="maxAttempts"/> consecutive failures
    /// the account is locked until <c>utcNow + <paramref name="lockoutDuration"/></c> and the
    /// counter is reset so a further run of failures starts a fresh lockout window.
    /// </summary>
    public void RecordFailedLoginAttempt(int maxAttempts, TimeSpan lockoutDuration, DateTimeOffset utcNow)
    {
        if (IsLockedOut(utcNow))
        {
            return;
        }

        FailedLoginAttempts++;

        if (FailedLoginAttempts < maxAttempts)
        {
            return;
        }

        FailedLoginAttempts = 0;
        LockedUntilUtc = utcNow + lockoutDuration;
    }

    public void ResetLoginAttempts()
    {
        FailedLoginAttempts = 0;
        LockedUntilUtc = null;
    }

    /// <summary>Rotates the security stamp, invalidating previously issued sessions.</summary>
    public void RegenerateSecurityStamp() => SecurityStamp = NewSecurityStamp();

    public void EnableTwoFactor(string verifiedPhoneE164, DateTimeOffset utcNow)
    {
        PhoneNumber = verifiedPhoneE164;
        PhoneNumberVerified = true;
        TwoFactorEnabled = true;
        TwoFactorEnabledAtUtc = utcNow;
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorEnabledAtUtc = null;
    }

    public void SetPhoneNumberVerified(string verifiedPhoneE164)
    {
        PhoneNumber = verifiedPhoneE164;
        PhoneNumberVerified = true;
    }

    private static string NewSecurityStamp() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static Result<User> Create(Guid id, Guid tenantId, string email, string firstName, string lastName, string passwordHash, string? phoneNumber = null, string? whatsappNumber = null)
    {
        if (id == Guid.Empty)
        {
            return UserErrors.IdRequired;
        }
        if (tenantId == Guid.Empty)
        {
            return UserErrors.TenantRequired;
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            return UserErrors.EmailRequired;
        }
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return UserErrors.PasswordRequired;
        }
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return UserErrors.FirstNameRequired;
        }
        if (string.IsNullOrWhiteSpace(lastName))
        {
            return UserErrors.LastNameRequired;
        }

        var user = new User(id, tenantId, email, firstName, lastName, passwordHash, phoneNumber, whatsappNumber);

        user.Raise(new UserRegisteredDomainEvent(user.Id, user.TenantId));

        return user;
    }
    public Result<Updated> Update(string firstName, string lastName, string? passwordHash, string? phoneNumber = null, string? whatsappNumber = null)
    {

        if (string.IsNullOrWhiteSpace(firstName))
        {
            return UserErrors.FirstNameRequired;
        }
        if (string.IsNullOrWhiteSpace(lastName))
        {
            return UserErrors.LastNameRequired;
        }
        if (!string.IsNullOrWhiteSpace(passwordHash))
        {
            PasswordHash = passwordHash;
        }
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        WhatsappNumber = string.IsNullOrWhiteSpace(whatsappNumber) ? null : whatsappNumber.Trim();
        return Result.Updated;
    }
}
