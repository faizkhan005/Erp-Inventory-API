using ERPInventoryApi.Application.DTOs;
using ERPInventoryApi.Domain.Entities;

namespace ERPInventoryApi.Application.Interfaces;

public interface IWarehouseRepository
{
    Task CreateWarehouse(Warehouse newWarehouse);

    Task UpdateWarehouse(Warehouse newWarehouse);

    Task<List<Warehouse>> GetAllWarehouse();

    Task<Warehouse?> GetById(Guid warehouseID);
    
    Task DeleteById(Guid warehouseID);

    Task<PagedResult<Warehouse>> GetPagedAsync(WarehouseQueryParams queryParams);
}