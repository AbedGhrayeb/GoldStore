// <copyright file="IDomainEventHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SharedKernel;

public interface IDomainEventHandler<in T>
    where T : IDomainEvent
{
    Task Handle(T domainEvent, CancellationToken cancellationToken);
}
