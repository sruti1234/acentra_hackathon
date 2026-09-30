using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantInventory.Api.Models;

namespace MultiTenantInventory.Api.Data;

// Development seed data. Each part only runs if its data is missing, so it is safe on every startup.
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        try
        {
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogWarning("Skipping seed: database has pending migrations. Run 'dotnet ef database update' first.");
                return;
            }

            await SeedTenantsAsync(db);
            await SeedUsersAsync(db, hasher);
            await SeedProductsAsync(db);

            logger.LogInformation("Development seed data is in place.");
        }
        catch (Exception ex)
        {
            // Don't crash the API if PostgreSQL is down — /api/health will still work.
            logger.LogError(ex, "Database seeding failed. Check the PostgreSQL connection string.");
        }
    }

    private static async Task SeedTenantsAsync(ApplicationDbContext db)
    {
        if (await db.Tenants.AnyAsync())
        {
            return;
        }

        db.Tenants.AddRange(
            new Tenant { Name = "ABC Corporation", Subdomain = "abc" },
            new Tenant { Name = "XYZ Technologies", Subdomain = "xyz" },
            new Tenant { Name = "Demo Industries", Subdomain = "demo" });

        await db.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(ApplicationDbContext db, IPasswordHasher<User> hasher)
    {
        var tenants = await db.Tenants.ToDictionaryAsync(t => t.Subdomain, t => t.Id);

        // Demo user: member of ALL three organizations.
        await EnsureUserAsync(db, hasher, "Demo User", "demo@example.com", "Demo@123", tenants.Values);

        // ABC-only user: used to prove that a member of tenant 1 gets 403 for tenant 2.
        if (tenants.TryGetValue("abc", out var abcId))
        {
            await EnsureUserAsync(db, hasher, "ABC Staff", "abc@example.com", "Abc@123", new[] { abcId });
        }
    }

    private static async Task EnsureUserAsync(
        ApplicationDbContext db, IPasswordHasher<User> hasher,
        string name, string email, string password, IEnumerable<int> tenantIds)
    {
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return;
        }

        var user = new User { Name = name, Email = email };
        user.PasswordHash = hasher.HashPassword(user, password);

        foreach (var tenantId in tenantIds)
        {
            user.UserTenants.Add(new UserTenant { TenantId = tenantId });
        }

        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(ApplicationDbContext db)
    {
        // No tenant is set during startup, so the global filter would hide every product.
        // IgnoreQueryFilters() is used here ONLY for this system-level seeding check.
        if (await db.Products.IgnoreQueryFilters().AnyAsync())
        {
            return;
        }

        var tenants = await db.Tenants.ToDictionaryAsync(t => t.Subdomain, t => t.Id);

        var seed = new Dictionary<string, (string Name, string Description, int Quantity, decimal Price)[]>
        {
            ["abc"] = new[]
            {
                ("Laptop", "14\" business laptop, 16 GB RAM, 512 GB SSD", 25, 68999m),
                ("Mouse", "Wireless optical mouse", 150, 799m),
                ("Keyboard", "Mechanical keyboard, US layout", 8, 3499m),
                ("Monitor", "27\" 4K IPS monitor", 0, 24999m)
            },
            ["xyz"] = new[]
            {
                ("Smartphone", "6.5\" Android smartphone, 128 GB", 40, 21999m),
                ("Charger", "65 W USB-C fast charger", 200, 1499m),
                ("Headphones", "Noise-cancelling over-ear headphones", 6, 8999m),
                ("Tablet", "10.9\" tablet, Wi-Fi, 64 GB", 18, 32999m)
            },
            ["demo"] = new[]
            {
                ("Printer", "Laser printer, duplex, Wi-Fi", 12, 15999m),
                ("Scanner", "Flatbed document scanner", 5, 9499m),
                ("Projector", "Full HD office projector", 3, 45999m),
                ("Router", "Dual-band Wi-Fi 6 router", 60, 4299m)
            }
        };

        foreach (var (subdomain, products) in seed)
        {
            if (!tenants.TryGetValue(subdomain, out var tenantId))
            {
                continue;
            }

            foreach (var p in products)
            {
                db.Products.Add(new Product
                {
                    Name = p.Name,
                    Description = p.Description,
                    Quantity = p.Quantity,
                    Price = p.Price,
                    TenantId = tenantId
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
