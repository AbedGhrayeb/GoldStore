using System.Text.Json;
using Application.Abstractions.Messaging;
using SharedKernel.Result;

namespace Application.Features.TenantSettings.UpdateTenantSettings;

public sealed record UpdateTenantSettingsCommand(JsonElement Settings) : ICommand<Updated>;
