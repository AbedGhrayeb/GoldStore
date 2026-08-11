namespace Application.Abstractions.Authentication;

public interface IUserContext
{
    /// <summary>True when an authenticated user is available in the current scope.</summary>
    bool IsAvailable { get; }

    /// <summary>The current user id. Throws when <see cref="IsAvailable"/> is false.</summary>
    Guid UserId { get; }
}
