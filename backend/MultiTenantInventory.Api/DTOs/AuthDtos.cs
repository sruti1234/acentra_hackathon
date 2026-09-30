using System.ComponentModel.DataAnnotations;

namespace MultiTenantInventory.Api.DTOs;

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public record UserDto(int Id, string Name, string Email);

public record TenantSummaryDto(int Id, string Name);

public record LoginResponse(string Token, UserDto User, List<TenantSummaryDto> Tenants);
