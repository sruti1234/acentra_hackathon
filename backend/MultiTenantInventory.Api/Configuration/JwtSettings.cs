namespace MultiTenantInventory.Api.Configuration;

// Bound from the "Jwt" configuration section. Key comes from user-secrets / environment variables.
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "MultiTenantInventory";
    public string Audience { get; set; } = "MultiTenantInventory.Client";
    public int ExpiresMinutes { get; set; } = 120;
}
