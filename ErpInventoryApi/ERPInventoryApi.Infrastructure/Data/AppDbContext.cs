using ERPInventoryApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERPInventoryApi.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global query filter - soft delete
        // IsDeleted = true records are invisible to all queries automatically
        modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Category>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Warehouse>().HasQueryFilter(w => !w.IsDeleted);

        // Product config
        modelBuilder.Entity<Product>(entity =>
        {   
            entity.HasKey(p => p.ID);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.SKU).IsRequired().HasMaxLength(50);
            entity.HasIndex(p => p.SKU).IsUnique();
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");

            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Warehouse)
                  .WithMany(w => w.Products)
                  .HasForeignKey(p => p.WarehouseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Category config
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.ID);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
        });

        // Warehouse config
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(w => w.ID);
            entity.Property(w => w.Name).IsRequired().HasMaxLength(100);
            entity.Property(w => w.Location).IsRequired().HasMaxLength(200);
        });
    }
}
