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

    /// <summary>
    /// Session/token version (plan Phase 4). Every issued cookie, access token, and
    /// refresh token carries this value; changing it invalidates all previously
    /// issued sessions. Regenerate on password change and on account disable.
    /// </summary>
    public string SecurityStamp { get; private set; }

    public User()
    {
        Email = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        PasswordHash = string.Empty;
        SecurityStamp = string.Empty;
    }
    public User(Guid id, Guid tenantId, string email, string firstName, string lastName, string passwordHash) : base(id)
    {
        TenantId = tenantId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PasswordHash = passwordHash;
        SecurityStamp = NewSecurityStamp();

    }

    /// <summary>Rotates the security stamp, invalidating previously issued sessions.</summary>
    public void RegenerateSecurityStamp() => SecurityStamp = NewSecurityStamp();

    private static string NewSecurityStamp() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static Result<User> Create(Guid id, Guid tenantId, string email, string firstName, string lastName, string passwordHash)
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

        return new User(id, tenantId, email, firstName, lastName, passwordHash);
    }
    public Result<Updated> Update(string firstName, string lastName, string? passwordHash)
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
        return Result.Updated;
    }
}
