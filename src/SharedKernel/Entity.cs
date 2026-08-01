using System.ComponentModel.DataAnnotations.Schema;

namespace SharedKernel;

public abstract class Entity
{
    public Guid Id { get; }
    public bool IsActive { get; set; } = true;

    private readonly List<IDomainEvent> _domainEvents = [];
    [NotMapped]
    public List<IDomainEvent> DomainEvents => [.. _domainEvents];


    protected Entity() { }
    protected Entity(Guid id) => Id = id == Guid.Empty ? Guid.CreateVersion7() : id;

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }
    public void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
