using ERPInventoryApi.Application.DTOs;
using ERPInventoryApi.Application.Interfaces;
using ERPInventoryApi.Domain.Entities;
using ERPInventoryApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERPInventoryApi.Infrastructure.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly AppDbContext _dbContext;

    public WarehouseRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task CreateWarehouse(Warehouse newWarehouse)
    {
        await _dbContext.Warehouses.AddAsync(newWarehouse);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteById(Guid warehouseID)
    {
        Warehouse wareHouse = await _dbContext.Warehouses.FirstOrDefaultAsync(w => w.ID == warehouseID) ?? throw new Exception("Entity Not Found");
        wareHouse.IsDeleted = true;
        wareHouse.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    public Task<List<Warehouse>> GetAllWarehouse()
    {
        return _dbContext.Warehouses.ToListAsync();
    }

    public Task<Warehouse?> GetById(Guid warehouseID)
    {
        return _dbContext.Warehouses.FirstOrDefaultAsync(w => w.ID == warehouseID);
    }

    public async Task UpdateWarehouse(Warehouse newWarehouse)
    {
        Warehouse warehouse = await _dbContext.Warehouses.FirstOrDefaultAsync(w => w.ID == newWarehouse.ID) ?? throw new Exception("Entity Not Found");
        warehouse.Name = newWarehouse.Name ?? warehouse.Name;
        warehouse.Location = newWarehouse.Location ?? warehouse.Location;
        warehouse.Capacity = newWarehouse.Capacity != 0 ? newWarehouse.Capacity : warehouse.Capacity;
        warehouse.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<PagedResult<Warehouse>> GetPagedAsync(WarehouseQueryParams q)
    {
        var query = _dbContext.Warehouses
            .Include(w => w.Products)   // for ProductCount
            .AsQueryable();

        // Filtering 
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var search = q.Search.Trim().ToLower();
            query = query.Where(w =>
                w.Name.ToLower().Contains(search) ||
                w.Location.ToLower().Contains(search));
        }

        if (q.MinCapacity.HasValue)
            query = query.Where(w => w.Capacity >= q.MinCapacity.Value);

        if (q.MaxCapacity.HasValue)
            query = query.Where(w => w.Capacity <= q.MaxCapacity.Value);

        // Sorting 
        var isAsc = q.SortOrder.ToLower() == "asc";

        IOrderedQueryable<Warehouse> orderedQuery = q.SortBy.ToLower() switch
        {
            "name" => isAsc ? query.OrderBy(w => w.Name)
                                : query.OrderByDescending(w => w.Name),
            "capacity" => isAsc ? query.OrderBy(w => w.Capacity)
                                : query.OrderByDescending(w => w.Capacity),
            _ => isAsc ? query.OrderBy(w => w.CreatedAt)
                                : query.OrderByDescending(w => w.CreatedAt)
        };

        orderedQuery = orderedQuery.ThenBy(w => w.ID);
        query = orderedQuery;

        // Cursor 
        if (q.Cursor.HasValue)
        {
            var cursorItem = await _dbContext.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.ID == q.Cursor.Value);

            if (cursorItem != null)
            {
                query = q.SortBy.ToLower() switch
                {
                    "name" => isAsc
                        ? query.Where(w => w.Name.CompareTo(cursorItem.Name) > 0
                            || (w.Name == cursorItem.Name && w.ID > cursorItem.ID))
                        : query.Where(w => w.Name.CompareTo(cursorItem.Name) < 0
                            || (w.Name == cursorItem.Name && w.ID > cursorItem.ID)),

                    "capacity" => isAsc
                        ? query.Where(w => w.Capacity > cursorItem.Capacity
                            || (w.Capacity == cursorItem.Capacity && w.ID > cursorItem.ID))
                        : query.Where(w => w.Capacity < cursorItem.Capacity
                            || (w.Capacity == cursorItem.Capacity && w.ID > cursorItem.ID)),

                    _ => isAsc
                        ? query.Where(w => w.CreatedAt > cursorItem.CreatedAt
                            || (w.CreatedAt == cursorItem.CreatedAt && w.ID > cursorItem.ID))
                        : query.Where(w => w.CreatedAt < cursorItem.CreatedAt
                            || (w.CreatedAt == cursorItem.CreatedAt && w.ID > cursorItem.ID))
                };
            }
        }

        var items = await query
            .AsNoTracking()
            .Take(q.PageSize + 1)
            .ToListAsync();

        var hasNextPage = items.Count > q.PageSize;
        if (hasNextPage) items.RemoveAt(items.Count - 1);

        return new PagedResult<Warehouse>
        {
            Items = items,
            NextCursor = hasNextPage ? items[^1].ID : null
        };
    }
}
