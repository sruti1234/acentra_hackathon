using System.ComponentModel.DataAnnotations;

namespace MultiTenantInventory.Api.DTOs;

// Used for both create and update. Deliberately has NO TenantId —
// the tenant always comes from the validated X-Tenant-ID (TenantContext).
public class ProductRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0, 1_000_000)]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0", "10000000")]
    public decimal Price { get; set; }
}

public record ProductDto(
    int Id,
    string Name,
    string? Description,
    int Quantity,
    decimal Price,
    string? ImageKey,
    string? ImageUrl,
    int TenantId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
