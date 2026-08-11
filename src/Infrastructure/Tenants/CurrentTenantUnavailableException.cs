namespace Infrastructure.Tenants;

public sealed class CurrentTenantUnavailableException : Exception
{
    public CurrentTenantUnavailableException()
        : base("No tenant is available in the current context. Tenant endpoints require an authenticated user with tenant claims; host and background flows must select a tenant explicitly.")
    {
    }
}
