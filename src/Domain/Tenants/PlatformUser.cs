using SharedKernel;
using SharedKernel.Result;

namespace Domain.Tenants;

public sealed class PlatformUser : Entity
{
    public string Email { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string PasswordHash { get; private set; }

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
}
