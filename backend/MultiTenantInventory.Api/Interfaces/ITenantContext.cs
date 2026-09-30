namespace MultiTenantInventory.Api.Interfaces;

// Holds the tenant for the CURRENT HTTP request only (registered as Scoped).
public interface ITenantContext
{
    int? TenantId { get; }

    void SetTenant(int tenantId);
}
