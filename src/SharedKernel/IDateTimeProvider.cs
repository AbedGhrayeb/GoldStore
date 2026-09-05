// <copyright file="IDateTimeProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SharedKernel;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    DateTime Now { get; }
}
