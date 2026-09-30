namespace MultiTenantInventory.Api.Models;

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Hashed with ASP.NET Core Identity's PasswordHasher (PBKDF2). Never the plain password.
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
}
