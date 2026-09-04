namespace Application.Abstractions.Authentication;

public interface IUserContext
{
    Guid UserId { get; }

    /// <summary>
    /// Non-throwing variant of <see cref="UserId"/>: returns null when no user is
    /// authenticated (background services, seeders, system writes).
    /// </summary>
    Guid? UserIdOrNull { get; }
}
