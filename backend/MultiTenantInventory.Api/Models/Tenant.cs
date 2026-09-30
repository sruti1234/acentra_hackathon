namespace MultiTenantInventory.Api.Models;

// An organization that uses the platform.
public class Tenant
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Short unique key for the tenant, e.g. "abc" -> abc.yourapp.com
    public string Subdomain { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Always stored in UTC (PostgreSQL "timestamp with time zone").
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Product> Products { get; set; } = new List<Product>();

    public ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
}
