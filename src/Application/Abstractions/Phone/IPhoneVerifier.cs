// <copyright file="IPhoneVerifier.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Abstractions.Phone;

public sealed record VerifiedPhone(string E164);

public interface IPhoneVerifier
{
    Task<VerifiedPhone> VerifyAsync(string idToken, CancellationToken cancellationToken);
}
