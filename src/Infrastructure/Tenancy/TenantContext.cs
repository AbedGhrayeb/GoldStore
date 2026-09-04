using Application.Abstractions.Tenancy;
using Domain.Tenants;

namespace Infrastructure.Tenancy;

/// <summary>
/// Scoped tenant holder. Set once per request by TenantResolutionMiddleware,
/// or explicitly per scope by background services (e.g. the migration runner).
/// </summary>
internal sealed class TenantContext : ITenantContext, ITenantContextSetter
{
    private TenantInfo? _current;

    public bool IsResolved => _current is not null;

    public bool IsReadOnly { get; private set; }

    public Guid TenantId => Current.TenantId;

    public string Subdomain => Current.Subdomain;

    public string SchemaName => Current.SchemaName;

    public TenantStatus Status => Current.Status;

    public string? ConnectionString => Current.ConnectionString;

    private TenantInfo Current => _current ?? throw new TenantContextUnavailableException();

    public void Set(TenantInfo tenant, bool isReadOnly)
    {
        _current = tenant;
        IsReadOnly = isReadOnly;
    }
}
