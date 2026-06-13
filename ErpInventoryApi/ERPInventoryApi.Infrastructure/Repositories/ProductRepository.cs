using ERPInventoryApi.Application.Interfaces;
using ERPInventoryApi.Domain.Entities;
using ERPInventoryApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERPInventoryApi.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _dbContext;
    public ProductRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task CreateNewProduct(Product newProd)
    {
        await _dbContext.Products.AddAsync(newProd);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteById(Guid id)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.ID == id) ?? throw new Exception("Entity Not Found");
        product.IsDeleted = true;
        product.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    public Task<List<Product>> GetAll()
    {
        return _dbContext.Products.ToListAsync();
    }

    public Task<Product?> GetById(Guid id)
    {
        return _dbContext.Products.FirstOrDefaultAsync(p => p.ID == id) ?? throw new Exception("Entity Not Found");
    }

    public async Task UpdateProduct(Product newProduct)
    {
        Product prod = await _dbContext.Products.FirstOrDefaultAsync(p => p.ID == newProduct.ID) ?? throw new Exception("Entity Not Found");
        prod.Name = newProduct.Name ?? prod.Name;
        prod.Description = newProduct.Description ?? prod.Description;
        prod.Price = newProduct.Price != 0 ? newProduct.Price : prod.Price;
        prod.StockQuantity = newProduct.StockQuantity != 0 ? newProduct.StockQuantity : prod.StockQuantity;
        prod.ReorderPoint = newProduct.ReorderPoint != 0 ? newProduct.ReorderPoint : prod.ReorderPoint;
        prod.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }
}
