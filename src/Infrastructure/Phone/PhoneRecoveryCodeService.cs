// <copyright file="PhoneRecoveryCodeService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Phone;

namespace Infrastructure.Phone;

internal sealed class PhoneRecoveryCodeService : IPhoneRecoveryCodeService
{
    public IReadOnlyList<string> GeneratePlainCodes(int count = 8)
    {
        List<string> codes = new(count);
        for (int i = 0; i < count; i++)
        {
            codes.Add(RandomNumberGenerator.GetString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 8));
        }

        return codes;
    }

    public string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim().ToUpperInvariant())));

    public bool Verify(string plainCode, string hash)
    {
        string computed = this.Hash(plainCode);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(hash));
    }
}
