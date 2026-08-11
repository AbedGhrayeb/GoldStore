namespace Infrastructure.Tenants;

/// <summary>
/// Raised by the central write guard when a write would cross tenant boundaries:
/// creating a row without a tenant, stamping a row with a foreign tenant, altering
/// a row's tenant, or modifying/deleting another tenant's row.
/// </summary>
public sealed class TenantAccessViolationException : Exception
{
    public TenantAccessViolationException(string message) : base(message)
    {
    }
}
