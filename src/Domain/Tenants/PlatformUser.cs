using SharedKernel;
using SharedKernel.Result;

namespace Domain.Tenants;

public sealed class PlatformUser : Entity
{
    public string Email { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string PasswordHash { get; private set; }

    /// <summary>Consecutive failed sign-in attempts since the last success (M7 lockout).</summary>
    public int FailedLoginAttempts { get; private set; }

    /// <summary>Instant until which the account is locked out (M7 lockout). Null = not locked.</summary>
    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public const int MaxFailedLoginAttempts = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private PlatformUser()
    {
        Email = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        PasswordHash = string.Empty;
    }

    private PlatformUser(Guid id, string email, string firstName, string lastName, string passwordHash) : base(id)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PasswordHash = passwordHash;
    }

    public static Result<PlatformUser> Create(string email, string firstName, string lastName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return TenantErrors.EmailRequired;
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return TenantErrors.PasswordRequired;
        }

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return TenantErrors.NameRequired;
        }

        return new PlatformUser(Guid.CreateVersion7(), email.Trim().ToLowerInvariant(), firstName.Trim(), lastName.Trim(), passwordHash);
    }

    public bool IsLockedOut(DateTimeOffset utcNow) => LockedUntilUtc is not null && LockedUntilUtc > utcNow;

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
}
