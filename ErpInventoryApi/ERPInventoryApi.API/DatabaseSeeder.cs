using Bogus;
using ERPInventoryApi.Domain.Entities;
using ERPInventoryApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERPInventoryApi.API;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        // Run migrations automatically on startup
        await db.Database.MigrateAsync();

        // Skip if data already exists
        if (await db.Products.AnyAsync())
        {
            logger.LogInformation("Database already seeded — skipping.");
            return;
        }

        logger.LogInformation("Seeding database...");

        // Seed Users 
        var adminPasswordHash = HashPassword("Admin@1234");
        var userPasswordHash = HashPassword("User@1234");

        var users = new List<User>
        {
            new() { Username = "admin",   PasswordHash = adminPasswordHash, Role = "Admin" },
            new() { Username = "faizan",  PasswordHash = userPasswordHash,  Role = "User"  },
            new() { Username = "testuser",PasswordHash = userPasswordHash,  Role = "User"  }
        };

        await db.Users.AddRangeAsync(users);
        await db.SaveChangesAsync();

        // Seed Categories
        var categoryNames = new[]
        {
            "Laptops", "Monitors", "Keyboards", "Mice", "Headsets",
            "Networking", "Storage", "Servers", "Printers", "Accessories"
        };

        var categoryFaker = new Faker<Category>()
            .RuleFor(c => c.ID, _ => Guid.NewGuid())
            .RuleFor(c => c.Name, (f, c) => categoryNames[f.IndexFaker % categoryNames.Length])
            .RuleFor(c => c.Description, f => f.Commerce.ProductDescription())
            .RuleFor(c => c.CreatedAt, f => DateTime.SpecifyKind(f.Date.Past(2), DateTimeKind.Utc))
            .RuleFor(c => c.UpdatedAt, (f, c) => c.CreatedAt);
                var categories = categoryFaker.Generate(categoryNames.Length);
        // Ensure unique names
        for (var i = 0; i < categories.Count; i++)
            categories[i].Name = categoryNames[i];

        await db.Categories.AddRangeAsync(categories);
        await db.SaveChangesAsync();

        // Seed Warehouses 
        var warehouseFaker = new Faker<Warehouse>()
            .RuleFor(w => w.ID, _ => Guid.NewGuid())
            .RuleFor(w => w.Name, f => $"{f.Address.City()} Warehouse")
            .RuleFor(w => w.Location, f => f.Address.FullAddress())
            .RuleFor(w => w.Capacity, f => f.Random.Int(500, 10000))
            .RuleFor(w => w.CreatedAt, f => DateTime.SpecifyKind(f.Date.Past(2), DateTimeKind.Utc))
            .RuleFor(w => w.UpdatedAt, (f, w) => w.CreatedAt);

        var warehouses = warehouseFaker.Generate(5);
        await db.Warehouses.AddRangeAsync(warehouses);
        await db.SaveChangesAsync();

        // Seed Products 
        var categoryIds = categories.Select(c => c.ID).ToList();
        var warehouseIds = warehouses.Select(w => w.ID).ToList();

        var productFaker = new Faker<Product>()
            .RuleFor(p => p.ID, _ => Guid.NewGuid())
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.SKU, f => f.Commerce.Ean13())
            .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
            .RuleFor(p => p.Price, f => Math.Round(f.Random.Decimal(5, 2000), 2))
            .RuleFor(p => p.StockQuantity, f => f.Random.Int(0, 500))
            .RuleFor(p => p.ReorderPoint, f => f.Random.Int(5, 50))
            .RuleFor(p => p.CategoryId, f => f.PickRandom(categoryIds))
            .RuleFor(p => p.WarehouseId, f => f.PickRandom(warehouseIds))
            .RuleFor(p => p.CreatedAt, f => DateTime.SpecifyKind(f.Date.Past(2), DateTimeKind.Utc))
            .RuleFor(p => p.UpdatedAt, (f, p) => DateTime.SpecifyKind(f.Date.Between(p.CreatedAt, DateTime.UtcNow), DateTimeKind.Utc));

        // Generate in batches of 100 to avoid memory spikes
        const int total = 10000;
        const int batchSize = 500;

        for (var i = 0; i < total; i += batchSize)
        {
            var batch = productFaker.Generate(batchSize);
            await db.Products.AddRangeAsync(batch);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded products {From}-{To} of {Total}", i + 1, i + batchSize, total);
        }

        logger.LogInformation("Seeding complete — {Categories} categories, {Warehouses} warehouses, {Products} products, {Users} users",
            categories.Count, warehouses.Count, total, users.Count);
    }

    // Same hashing logic as AuthService
    private static string HashPassword(string password)
    {
        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var saltedPassword = salt.Concat(System.Text.Encoding.UTF8.GetBytes(password)).ToArray();
        var hash = System.Security.Cryptography.SHA256.HashData(saltedPassword);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
}
