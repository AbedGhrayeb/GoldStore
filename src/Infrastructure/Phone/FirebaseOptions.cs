// <copyright file="FirebaseOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Infrastructure.Phone;

public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";

    public string ProjectId { get; set; } = string.Empty;

    public string WebApiKey { get; set; } = string.Empty;

    public string AuthDomain { get; set; } = string.Empty;

    public string AppId { get; set; } = string.Empty;

    public string ServiceAccountJsonPath { get; set; } = string.Empty;

    public bool UseEmulator { get; set; }

    public string EmulatorHost { get; set; } = "localhost:9099";
}
