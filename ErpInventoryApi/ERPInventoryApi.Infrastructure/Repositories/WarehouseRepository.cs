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
}
