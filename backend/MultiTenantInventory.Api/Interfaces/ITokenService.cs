using MultiTenantInventory.Api.Models;

namespace MultiTenantInventory.Api.Interfaces;

public interface ITokenService
{
    string CreateToken(User user);
}
