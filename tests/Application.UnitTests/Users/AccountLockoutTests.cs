// <copyright file="AccountLockoutTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Users;
using Xunit;

namespace Application.UnitTests.Users;

/// <summary>
/// Pins the account-lockout state machine (plan M7-B1): five consecutive failures lock
/// the account for 15 minutes, further failures while locked are ignored, a correct
/// password resets the counter, and an expired lockout lifts.
/// </summary>
public sealed class AccountLockoutTests
{
    private const string Email = "lockout@goldstore.test";
    private const string PasswordHash = "hash";
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public void RecordFailedLoginAttempt_BelowMaxAttempts_CountsButDoesNotLock()
    {
        User user = CreateUser();
        DateTimeOffset now = new(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);

        for (int attempt = 0; attempt < User.MaxFailedLoginAttempts - 1; attempt++)
        {
            user.RecordFailedLoginAttempt(User.MaxFailedLoginAttempts, User.LockoutDuration, now);
        }

        Assert.False(user.IsLockedOut(now));
        Assert.Equal(User.MaxFailedLoginAttempts - 1, user.FailedLoginAttempts);
        Assert.Null(user.LockedUntilUtc);
    }

    [Fact]
    public void RecordFailedLoginAttempt_FifthFailure_LocksAccountAndResetsCounter()
    {
        User user = CreateUser();
        DateTimeOffset now = new(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);

        for (int attempt = 0; attempt < User.MaxFailedLoginAttempts; attempt++)
        {
            user.RecordFailedLoginAttempt(User.MaxFailedLoginAttempts, User.LockoutDuration, now);
        }

        Assert.True(user.IsLockedOut(now));
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Equal(now + User.LockoutDuration, user.LockedUntilUtc);
    }

    [Fact]
    public void RecordFailedLoginAttempt_WhileLocked_IsIgnored()
    {
        User user = CreateUser();
        DateTimeOffset now = new(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);
        for (int attempt = 0; attempt < User.MaxFailedLoginAttempts; attempt++)
        {
            user.RecordFailedLoginAttempt(User.MaxFailedLoginAttempts, User.LockoutDuration, now);
        }

        Assert.True(user.IsLockedOut(now));

        DateTimeOffset duringLockout = now.AddMinutes(5);
        user.RecordFailedLoginAttempt(User.MaxFailedLoginAttempts, User.LockoutDuration, duringLockout);

        Assert.True(user.IsLockedOut(duringLockout));
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Equal(now + User.LockoutDuration, user.LockedUntilUtc);
    }

    [Fact]
    public void IsLockedOut_AfterDurationElapses_ReturnsFalse()
    {
        User user = CreateUser();
        DateTimeOffset now = new(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);
        user.RecordFailedLoginAttempt(User.MaxFailedLoginAttempts, User.LockoutDuration, now);

        Assert.False(user.IsLockedOut(now + User.LockoutDuration));
        Assert.False(user.IsLockedOut(now + User.LockoutDuration + TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void ResetLoginAttempts_ClearsCounterAndLockout()
    {
        User user = CreateUser();
        DateTimeOffset now = new(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);
        user.RecordFailedLoginAttempt(User.MaxFailedLoginAttempts, User.LockoutDuration, now);

        user.ResetLoginAttempts();

        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.Null(user.LockedUntilUtc);
        Assert.False(user.IsLockedOut(now));
    }

    private static User CreateUser() =>
        User.Create(Guid.NewGuid(), TenantId, Email, "Lockout", "Test", PasswordHash).Value;
}
