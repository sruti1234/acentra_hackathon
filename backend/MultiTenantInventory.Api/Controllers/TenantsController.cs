using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantInventory.Api.Configuration;
using MultiTenantInventory.Api.Data;
using MultiTenantInventory.Api.DTOs;
using MultiTenantInventory.Api.Interfaces;
using MultiTenantInventory.Api.Middleware;

namespace MultiTenantInventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IWebHostEnvironment _environment;

    public TenantsController(ApplicationDbContext db, ITenantContext tenantContext, IWebHostEnvironment environment)
    {
        _db = db;
        _tenantContext = tenantContext;
        _environment = environment;
    }

    // GET /api/tenants — Phase 1 development endpoint. Only available in Development.
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<TenantDto>>> GetAll()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var tenants = await _db.Tenants
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Select(t => new TenantDto(t.Id, t.Name, t.Subdomain, t.IsActive, t.CreatedAt))
            .ToListAsync();

        return Ok(tenants);
    }

    // GET /api/tenants/my-tenants — organizations the logged-in user belongs to.
    [HttpGet("my-tenants")]
    [Authorize]
    public async Task<ActionResult<List<TenantDto>>> GetMyTenants()
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var tenants = await _db.UserTenants
            .AsNoTracking()
            .Where(ut => ut.UserId == userId.Value && ut.Tenant!.IsActive)
            .OrderBy(ut => ut.TenantId)
            .Select(ut => new TenantDto(ut.Tenant!.Id, ut.Tenant.Name, ut.Tenant.Subdomain, ut.Tenant.IsActive, ut.Tenant.CreatedAt))
            .ToListAsync();

        return Ok(tenants);
    }

    // GET /api/tenants/current — the tenant selected by the validated X-Tenant-ID header.
    [HttpGet("current")]
    [Authorize]
    [RequireTenant]
    public async Task<ActionResult<TenantDto>> GetCurrent()
    {
        var tenantId = _tenantContext.TenantId!.Value;

        var tenant = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new TenantDto(t.Id, t.Name, t.Subdomain, t.IsActive, t.CreatedAt))
            .FirstOrDefaultAsync();

        return tenant is null ? NotFound() : Ok(tenant);
    }
}
