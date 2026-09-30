namespace MultiTenantInventory.Api.Models;

// Membership: which organizations (tenants) a user is allowed to access.
public class UserTenant
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }
}
