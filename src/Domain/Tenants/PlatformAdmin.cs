using SharedKernel;
using SharedKernel.Result;

namespace Domain.Tenants;

public sealed class PlatformAdmin : Entity
{
    public string Email { get; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string PasswordHash { get; private set; }

    private PlatformAdmin() { }

    private PlatformAdmin(Guid id, string email, string firstName, string lastName, string passwordHash)
        : base(id)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PasswordHash = passwordHash;
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
            PasswordHash = passwordHash;
        }

        FirstName = firstName;
        LastName = lastName;

        return Result.Updated;
    }
}
