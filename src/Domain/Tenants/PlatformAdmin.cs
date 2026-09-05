// <copyright file="PlatformAdmin.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel;
using SharedKernel.Result;

namespace Domain.Tenants;

public sealed class PlatformAdmin : Entity
{
    public string Email { get; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string PasswordHash { get; private set; }

    private PlatformAdmin()
    {
    }

    private PlatformAdmin(Guid id, string email, string firstName, string lastName, string passwordHash)
        : base(id)
    {
        this.Email = email;
        this.FirstName = firstName;
        this.LastName = lastName;
        this.PasswordHash = passwordHash;
    }

    public static Result<PlatformAdmin> Create(string email, string firstName, string lastName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return PlatformAdminErrors.EmailRequired;
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return PlatformAdminErrors.PasswordRequired;
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            return PlatformAdminErrors.FirstNameRequired;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return PlatformAdminErrors.LastNameRequired;
        }

        return new PlatformAdmin(Guid.CreateVersion7(), email, firstName, lastName, passwordHash);
    }

    public Result<Updated> Update(string firstName, string lastName, string? passwordHash)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return PlatformAdminErrors.FirstNameRequired;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return PlatformAdminErrors.LastNameRequired;
        }

        if (!string.IsNullOrWhiteSpace(passwordHash))
        {
            this.PasswordHash = passwordHash;
        }

        this.FirstName = firstName;
        this.LastName = lastName;

        return Result.Updated;
    }
}
