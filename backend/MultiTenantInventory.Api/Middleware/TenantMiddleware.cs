using Microsoft.EntityFrameworkCore;
using MultiTenantInventory.Api.Configuration;
using MultiTenantInventory.Api.Data;
using MultiTenantInventory.Api.Interfaces;

namespace MultiTenantInventory.Api.Middleware;

// Runs after authentication/authorization. For endpoints marked [RequireTenant] it:
//   1. requires an authenticated user
//   2. reads X-Tenant-ID
//   3. checks the tenant exists and is active
//   4. checks the user is a member of that tenant (the header alone is NEVER trusted)
//   5. stores the tenant in the scoped TenantContext
public class TenantMiddleware
{
    public const string TenantHeader = "X-Tenant-ID";

    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    // Scoped services are injected per request through InvokeAsync parameters.
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ApplicationDbContext db)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<RequireTenantAttribute>() is null)
        {
            await _next(context);
            return;
        }

        var userId = context.User.GetUserId();
        if (context.User.Identity?.IsAuthenticated != true || userId is null)
        {
            await WriteError(context, StatusCodes.Status401Unauthorized, "Authentication required.");
            return;
        }

        if (!context.Request.Headers.TryGetValue(TenantHeader, out var headerValue) || string.IsNullOrWhiteSpace(headerValue))
        {
            await WriteError(context, StatusCodes.Status400BadRequest, $"Missing {TenantHeader} header.");
            return;
        }

        if (!int.TryParse(headerValue.ToString(), out var tenantId) || tenantId <= 0)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, $"Invalid {TenantHeader} header.");
            return;
        }

        var tenant = await db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new { t.Id, t.IsActive })
            .FirstOrDefaultAsync(context.RequestAborted);

        if (tenant is null)
        {
            await WriteError(context, StatusCodes.Status404NotFound, "Tenant not found.");
            return;
        }

        if (!tenant.IsActive)
        {
            await WriteError(context, StatusCodes.Status403Forbidden, "This organization is inactive.");
            return;
        }

        var isMember = await db.UserTenants
            .AnyAsync(ut => ut.UserId == userId.Value && ut.TenantId == tenantId, context.RequestAborted);

        if (!isMember)
        {
            await WriteError(context, StatusCodes.Status403Forbidden, "You do not have access to this organization.");
            return;
        }

        tenantContext.SetTenant(tenantId);

        await _next(context);
    }

    private static Task WriteError(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new { status = statusCode, error = message });
    }
}
