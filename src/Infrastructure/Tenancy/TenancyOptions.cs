namespace Infrastructure.Tenancy;

public sealed class TenancyOptions
{
    public const string SectionName = "Tenancy";

    /// <summary>The base domain tenants are served from (e.g. "goldstore.com", "localhost" in dev).</summary>
    public string BaseDomain { get; set; } = "localhost";
}
