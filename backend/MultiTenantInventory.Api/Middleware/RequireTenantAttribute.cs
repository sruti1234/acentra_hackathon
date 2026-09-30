namespace MultiTenantInventory.Api.Middleware;

// Put this on a controller/action to make TenantMiddleware validate X-Tenant-ID for it.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireTenantAttribute : Attribute
{
}
