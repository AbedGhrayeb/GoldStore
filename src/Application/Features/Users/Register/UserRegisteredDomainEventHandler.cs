// <copyright file="UserRegisteredDomainEventHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Users;
using SharedKernel;

namespace Application.Users.Register;

/// <summary>
/// Handles <see cref="UserRegisteredDomainEvent"/>. The event carries the tenant id, so
/// the handler can scope any tenant-owned work from the event itself rather than an
/// ambient tenant that may be absent outside a request scope (plan Phase 5 item 6).
/// </summary>
internal sealed class UserRegisteredDomainEventHandler : IDomainEventHandler<UserRegisteredDomainEvent>
{
    public Task Handle(UserRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // TODO: Send an email verification link to the user at the tenant identified by
        // domainEvent.TenantId. Tenant-owned rows written here must be stamped by
        // ICurrentTenantSetter before SaveChanges.
        return Task.CompletedTask;
    }
}
