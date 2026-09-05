// <copyright file="IUserContext.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Abstractions.Authentication;

public interface IUserContext
{
    /// <summary>Gets a value indicating whether true when an authenticated user is available in the current scope.</summary>
    bool IsAvailable { get; }

    /// <summary>Gets the current user id. Throws when <see cref="IsAvailable"/> is false.</summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets non-throwing variant of <see cref="UserId"/>: returns null when no user is
    /// authenticated (background services, seeders, system writes).
    /// </summary>
    Guid? UserIdOrNull { get; }
}
