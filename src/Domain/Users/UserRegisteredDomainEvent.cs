using SharedKernel;

namespace Domain.Users;

/// <summary>
/// Raised when a tenant user is created. Carries the tenant context with the event so
/// handlers (email verification, welcome messages) never need to resolve a separate
/// ambient tenant: the tenant is derived from the event itself (plan Phase 5 item 6).
/// </summary>
public sealed record UserRegisteredDomainEvent(Guid UserId, Guid TenantId) : IDomainEvent;
