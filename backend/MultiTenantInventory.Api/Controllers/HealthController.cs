using Microsoft.AspNetCore.Mvc;
using MultiTenantInventory.Api.Data;

namespace MultiTenantInventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    // ApplicationDbContext is provided by Dependency Injection (registered in Program.cs).
    public HealthController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /api/health
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "API is running" });
    }

    // GET /api/health/database
    // Checks that the PostgreSQL connection string is correct and the server is reachable.
    [HttpGet("database")]
    public async Task<IActionResult> GetDatabase()
    {
        var canConnect = await _db.Database.CanConnectAsync();

        return canConnect
            ? Ok(new { database = "Connected" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { database = "Not Connected" });
    }
}
