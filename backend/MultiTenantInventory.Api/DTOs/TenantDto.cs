namespace MultiTenantInventory.Api.DTOs;

// What the API returns for a tenant (keeps the database entity separate from the API shape).
public record TenantDto(int Id, string Name, string Subdomain, bool IsActive, DateTime CreatedAt);
