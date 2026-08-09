using System.Text.Json;
using Application.Abstractions.Messaging;

namespace Application.Features.TenantSettings.GetTenantSettings;

public sealed record GetTenantSettingsQuery : IQuery<JsonElement>;
