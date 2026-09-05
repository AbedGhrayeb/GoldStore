// <copyright file="Entity.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations.Schema;

namespace SharedKernel;

public abstract class Entity
{
    public Guid Id { get; }

    public bool IsActive { get; set; } = true;

    private readonly List<IDomainEvent> domainEvents = [];

    [NotMapped]
    public List<IDomainEvent> DomainEvents => [.. this.domainEvents];

    protected Entity()
    {
    }

    protected Entity(Guid id) => this.Id = id == Guid.Empty ? Guid.CreateVersion7() : id;

    public void ClearDomainEvents()
    {
        this.domainEvents.Clear();
    }

    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        this.domainEvents.Remove(domainEvent);
    }

    public void Raise(IDomainEvent domainEvent)
    {
        this.domainEvents.Add(domainEvent);
    }
}
