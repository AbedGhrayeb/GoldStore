// <copyright file="IAuthSessionManager.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Users;

namespace Application.Abstractions.Authentication;

public interface IAuthSessionManager
{
    Task SignInAsync(string username, bool rememberMe, CancellationToken cancellationToken);

    Task SignOutAsync();
}
