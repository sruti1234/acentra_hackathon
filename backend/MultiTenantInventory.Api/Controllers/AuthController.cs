using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantInventory.Api.Data;
using MultiTenantInventory.Api.DTOs;
using MultiTenantInventory.Api.Interfaces;
using MultiTenantInventory.Api.Models;

namespace MultiTenantInventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthController(ApplicationDbContext db, ITokenService tokenService, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null
            || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var tenants = await _db.UserTenants
            .Where(ut => ut.UserId == user.Id && ut.Tenant!.IsActive)
            .OrderBy(ut => ut.TenantId)
            .Select(ut => new TenantSummaryDto(ut.Tenant!.Id, ut.Tenant.Name))
            .ToListAsync();

        var token = _tokenService.CreateToken(user);

        return Ok(new LoginResponse(token, new UserDto(user.Id, user.Name, user.Email), tenants));
    }
}
