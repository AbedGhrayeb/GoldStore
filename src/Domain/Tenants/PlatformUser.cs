// <copyright file="PlatformUser.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel;
using SharedKernel.Result;

namespace Domain.Tenants;

public sealed class PlatformUser : Entity
{
    public string Email { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string PasswordHash { get; private set; }

    public string? PhoneNumber { get; private set; }

    public bool TwoFactorEnabled { get; private set; }

    public bool PhoneNumberVerified { get; private set; }

    public DateTimeOffset? TwoFactorEnabledAtUtc { get; private set; }

    /// <summary>Gets consecutive failed sign-in attempts since the last success (M7 lockout).</summary>
    public int FailedLoginAttempts { get; private set; }

    /// <summary>Gets instant until which the account is locked out (M7 lockout). Null = not locked.</summary>
    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public const int MaxFailedLoginAttempts = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private PlatformUser()
    {
        this.Email = string.Empty;
        this.FirstName = string.Empty;
        this.LastName = string.Empty;
        this.PasswordHash = string.Empty;
    }

    private PlatformUser(Guid id, string email, string firstName, string lastName, string passwordHash)
        : base(id)
    {
        this.Email = email;
        this.FirstName = firstName;
        this.LastName = lastName;
        this.PasswordHash = passwordHash;
    }

    private PlatformUser(Guid id, string email, string firstName, string lastName, string passwordHash, string? phoneNumber)
        : base(id)
    {
        this.Email = email;
        this.FirstName = firstName;
        this.LastName = lastName;
        this.PasswordHash = passwordHash;
        this.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
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

    public bool IsLockedOut(DateTimeOffset utcNow) => this.LockedUntilUtc is not null && this.LockedUntilUtc > utcNow;

    public void RecordFailedLoginAttempt(int maxAttempts, TimeSpan lockoutDuration, DateTimeOffset utcNow)
    {
        if (this.IsLockedOut(utcNow))
        {
            return;
        }

        this.FailedLoginAttempts++;

        if (this.FailedLoginAttempts < maxAttempts)
        {
            return;
        }

        this.FailedLoginAttempts = 0;
        this.LockedUntilUtc = utcNow + lockoutDuration;
    }

    public void ResetLoginAttempts()
    {
        this.FailedLoginAttempts = 0;
        this.LockedUntilUtc = null;
    }

    public void EnableTwoFactor(string verifiedPhoneE164, DateTimeOffset utcNow)
    {
        this.PhoneNumber = verifiedPhoneE164;
        this.PhoneNumberVerified = true;
        this.TwoFactorEnabled = true;
        this.TwoFactorEnabledAtUtc = utcNow;
    }

    public void DisableTwoFactor()
    {
        this.TwoFactorEnabled = false;
        this.TwoFactorEnabledAtUtc = null;
    }

    public void SetPhoneNumberVerified(string verifiedPhoneE164)
    {
        this.PhoneNumber = verifiedPhoneE164;
        this.PhoneNumberVerified = true;
    }

    public void ChangePassword(string newPasswordHash)
    {
        this.PasswordHash = newPasswordHash;
    }
}
