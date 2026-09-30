using MultiTenantInventory.Api.Interfaces;

namespace MultiTenantInventory.Api.Services;

// One instance per HTTP request (Scoped) — no static or global tenant state.
public class TenantContext : ITenantContext
{
    public int? TenantId { get; private set; }

    public void SetTenant(int tenantId)
    {
        if (TenantId.HasValue && TenantId.Value != tenantId)
        {
            throw new InvalidOperationException("Tenant has already been set for this request.");
        }

        TenantId = tenantId;
    }
}
