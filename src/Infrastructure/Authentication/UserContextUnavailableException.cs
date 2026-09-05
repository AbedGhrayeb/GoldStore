// <copyright file="UserContextUnavailableException.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Infrastructure.Authentication;

public sealed class UserContextUnavailableException : Exception
{
    public UserContextUnavailableException()
        : base("User context is unavailable")
    {
    }
}
