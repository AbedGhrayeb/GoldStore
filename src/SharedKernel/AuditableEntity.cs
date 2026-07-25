namespace SharedKernel;

public abstract class AuditableEntity : Entity
{
    protected AuditableEntity() { }
    protected AuditableEntity(Guid id) : base(id) { }
}
