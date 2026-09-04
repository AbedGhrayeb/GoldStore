using Application.Abstractions.Messaging;
using Domain.Tenants;
using SharedKernel.Result;

namespace Application.Features.Platform.Tenants.ChangeTenantStatus;

public sealed record ChangeTenantStatusCommand(Guid TenantId, TenantStatus Status) : ICommand<Updated>;
