namespace Infrastructure.Tenancy;

public sealed class TenantContextUnavailableException : Exception
{
    public TenantContextUnavailableException() : base("Tenant context is unavailable")
    {
    }
}
