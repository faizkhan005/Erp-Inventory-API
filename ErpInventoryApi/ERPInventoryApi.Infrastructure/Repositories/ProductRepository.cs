using ERPInventoryApi.Application.DTOs;
using ERPInventoryApi.Application.Interfaces;
using ERPInventoryApi.Domain.Entities;
using ERPInventoryApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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

    public async Task<PagedResult<Product>> GetPagedAsync(ProductQueryParams q)
    {
        // Base query with navigation properties
        var query = _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Warehouse)
            .AsQueryable();

        // Filtering 
        if (q.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == q.CategoryId.Value);

        if (q.WarehouseId.HasValue)
            query = query.Where(p => p.WarehouseId == q.WarehouseId.Value);

        if (q.MinPrice.HasValue)
            query = query.Where(p => p.Price >= q.MinPrice.Value);

        if (q.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= q.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var search = q.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                p.SKU.ToLower().Contains(search) ||
                p.Description.ToLower().Contains(search));
        }

        //  Sorting 
        var isAsc = q.SortOrder.ToLower() == "asc";

        Expression<Func<Product, object>> keySelector = q.SortBy.ToLower() switch
        {
            "name" => p => p.Name,
            "price" => p => p.Price,
            "stockquantity" => p => p.StockQuantity,
            _ => p => p.CreatedAt
        };

        IOrderedQueryable<Product> orderedQuery = isAsc
            ? query.OrderBy(keySelector)
            : query.OrderByDescending(keySelector);

        // Cursor: always add Id as tiebreaker 
        // Without a tiebreaker, rows with identical sort values produce
        // inconsistent page boundaries across queries.
        orderedQuery = orderedQuery.ThenBy(p => p.ID);

        query = orderedQuery;

        // Apply cursor
        if (q.Cursor.HasValue)
        {
            // Find the cursor item so we know its sort key value
            var cursorItem = await _dbContext.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ID == q.Cursor.Value);

            if (cursorItem != null)
            {
                // Skip everything up to and including the cursor item
                // using the same sort field for consistency
                query = q.SortBy.ToLower() switch
                {
                    "name" => isAsc
                        ? query.Where(p => p.Name.CompareTo(cursorItem.Name) > 0
                            || (p.Name == cursorItem.Name && p.ID > cursorItem.ID))
                        : query.Where(p => p.Name.CompareTo(cursorItem.Name) < 0
                            || (p.Name == cursorItem.Name && p.ID > cursorItem.ID)),

                    "price" => isAsc
                        ? query.Where(p => p.Price > cursorItem.Price
                            || (p.Price == cursorItem.Price && p.ID > cursorItem.ID))
                        : query.Where(p => p.Price < cursorItem.Price
                            || (p.Price == cursorItem.Price && p.ID > cursorItem.ID)),

                    "stockquantity" => isAsc
                        ? query.Where(p => p.StockQuantity > cursorItem.StockQuantity
                            || (p.StockQuantity == cursorItem.StockQuantity && p.ID > cursorItem.ID))
                        : query.Where(p => p.StockQuantity < cursorItem.StockQuantity
                            || (p.StockQuantity == cursorItem.StockQuantity && p.ID > cursorItem.ID)),

                    _ => isAsc
                        ? query.Where(p => p.CreatedAt > cursorItem.CreatedAt
                            || (p.CreatedAt == cursorItem.CreatedAt && p.ID > cursorItem.ID))
                        : query.Where(p => p.CreatedAt < cursorItem.CreatedAt
                            || (p.CreatedAt == cursorItem.CreatedAt && p.ID > cursorItem.ID))
                };
            }
        }

        // Fetch pageSize + 1 to detect if there's a next page
        var items = await query
            .AsNoTracking()
            .Take(q.PageSize + 1)
            .ToListAsync();

        var hasNextPage = items.Count > q.PageSize;
        if (hasNextPage) items.RemoveAt(items.Count - 1); // remove the extra item

        return new PagedResult<Product>
        {
            Items = items,
            NextCursor = hasNextPage ? items[^1].ID : null
        };
    }
}
