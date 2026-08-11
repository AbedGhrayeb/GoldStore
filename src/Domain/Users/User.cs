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

    public User()
    {

    }
    public User(Guid id, Guid tenantId, string email, string firstName, string lastName, string passwordHash) : base(id)
    {
        TenantId = tenantId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PasswordHash = passwordHash;

    }

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
