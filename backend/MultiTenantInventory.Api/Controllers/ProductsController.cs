using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantInventory.Api.Data;
using MultiTenantInventory.Api.DTOs;
using MultiTenantInventory.Api.Interfaces;
using MultiTenantInventory.Api.Middleware;
using MultiTenantInventory.Api.Models;

namespace MultiTenantInventory.Api.Controllers;

// Every action needs a valid JWT AND a validated X-Tenant-ID.
// Queries below never filter by tenant manually — the EF Core global query filter does it.
[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireTenant]
public class ProductsController : ControllerBase
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IS3Service _s3;

    public ProductsController(ApplicationDbContext db, ITenantContext tenantContext, IS3Service s3)
    {
        _db = db;
        _tenantContext = tenantContext;
        _s3 = s3;
    }

    // GET /api/products
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll()
    {
        var products = await _db.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();

        return Ok(products.Select(ToDto).ToList());
    }

    // GET /api/products/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        // Another tenant's product is invisible here, so this returns 404 for it.
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return product is null ? NotFound(new { error = "Product not found." }) : Ok(ToDto(product));
    }

    // POST /api/products
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(ProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Quantity = request.Quantity,
            Price = request.Price,
            // Tenant comes from the validated TenantContext — never from the client.
            TenantId = _tenantContext.TenantId!.Value,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToDto(product));
    }

    // PUT /api/products/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, ProductRequest request)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound(new { error = "Product not found." });
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.Quantity = request.Quantity;
        product.Price = request.Price;

        await _db.SaveChangesAsync();

        return Ok(ToDto(product));
    }

    // DELETE /api/products/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound(new { error = "Product not found." });
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        // Best-effort cleanup of the image; the product is already deleted.
        if (product.ImageKey is not null && _s3.IsConfigured)
        {
            try { await _s3.DeleteAsync(product.ImageKey); } catch (StorageException) { }
        }

        return NoContent();
    }

    // POST /api/products/5/image   (multipart/form-data, field name "file")
    [HttpPost("{id:int}/image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
    public async Task<ActionResult<ProductDto>> UploadImage(int id, IFormFile file)
    {
        if (!_s3.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Image storage (AWS S3) is not configured on the server. Set AWS:BucketName and AWS:Region." });
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "Please choose an image file." });
        }

        if (file.Length > MaxImageBytes)
        {
            return BadRequest(new { error = "Image must be 5 MB or smaller." });
        }

        if (!AllowedImageTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only JPEG, PNG, GIF or WebP images are allowed." });
        }

        // The global query filter guarantees this product belongs to the current tenant.
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound(new { error = "Product not found." });
        }

        // The S3 key is built ONLY on the server from the validated TenantContext:
        //   tenants/{tenantId}/products/{productId}/{filename}
        var tenantId = _tenantContext.TenantId!.Value;
        if (product.TenantId != tenantId)
        {
            return NotFound(new { error = "Product not found." }); // defence in depth; the query filter already guarantees this
        }

        var key = $"tenants/{tenantId}/products/{product.Id}/{SafeFileName(file.FileName)}";

        try
        {
            await using var stream = file.OpenReadStream();
            await _s3.UploadAsync(key, stream, file.ContentType);
        }
        catch (StorageException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }

        var oldKey = product.ImageKey;
        product.ImageKey = key;
        await _db.SaveChangesAsync();

        if (oldKey is not null && oldKey != key)
        {
            try { await _s3.DeleteAsync(oldKey); } catch (StorageException) { }
        }

        return Ok(ToDto(product));
    }

    private ProductDto ToDto(Product p) => new(
        p.Id,
        p.Name,
        p.Description,
        p.Quantity,
        p.Price,
        p.ImageKey,
        p.ImageKey is null ? null : _s3.GetPresignedUrl(p.ImageKey),
        p.TenantId,
        p.CreatedAt,
        p.UpdatedAt);

    // Strips any path the client sends ("../../x.jpg") and unsafe characters.
    private static string SafeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        name = Regex.Replace(name, @"[^a-zA-Z0-9._-]", "-");
        if (string.IsNullOrWhiteSpace(name) || name.Trim('.', '-').Length == 0)
        {
            name = "image";
        }

        return name.Length > 100 ? name[^100..] : name;
    }
}
