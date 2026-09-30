using System.Security.Claims;

namespace MultiTenantInventory.Api.Configuration;

public static class ClaimsPrincipalExtensions
{
    // The JWT "sub" claim holds the user's database Id.
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("sub")?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }
}
