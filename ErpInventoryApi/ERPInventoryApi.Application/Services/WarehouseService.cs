using ERPInventoryApi.Application.DTOs;
using ERPInventoryApi.Application.Helper;
using ERPInventoryApi.Application.Interfaces;
using ERPInventoryApi.Domain.Entities;

namespace ERPInventoryApi.Application.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICacheService _cache;

    public WarehouseService(IWarehouseRepository warehouseRepository, ICacheService cache)
    {
        _warehouseRepository = warehouseRepository;
        _cache = cache;
    }
    public async Task AddWarehouse(WarehouseRequestDto warehouseRequestDto)
    {
        if(!Validate(warehouseRequestDto))
            throw new Exception("Invalid Request");

        await _warehouseRepository.CreateWarehouse(new Warehouse
        {
            Name = warehouseRequestDto.Name,
            Location = warehouseRequestDto.Location,
            Capacity = warehouseRequestDto.Capacity
        });

        await _cache.RemoveByPrefixAsync(CacheKeys.WarehousesPrefix);
    }

    public async Task DeleteWarehouse(Guid warehouseID)
    {
        await _warehouseRepository.DeleteById(warehouseID);

        await _cache.RemoveAsync(CacheKeys.WarehouseById(warehouseID));
        await _cache.RemoveByPrefixAsync(CacheKeys.WarehousesPrefix);
    }

    public async Task<List<WarehouseResponseDto>> GetAll()
    {
        List<Warehouse> warehouses = await _warehouseRepository.GetAllWarehouse();
        List<WarehouseResponseDto> listWarehouse = [.. warehouses.Select(w => new WarehouseResponseDto(w.ID, w.Name, w.Location, w.Capacity, w.Products.Count,w.CreatedAt,w.UpdatedAt))];
        return listWarehouse;
    }

    public async Task<WarehouseResponseDto> GetById(Guid warehouseID)
    {
        var key = CacheKeys.WarehouseById(warehouseID);
        var cached = await _cache.GetAsync<WarehouseResponseDto>(key);
        if (cached is not null) return cached;

        Warehouse warehouse = await _warehouseRepository.GetById(warehouseID) ?? throw new Exception("Warehouse Not Found");
        WarehouseResponseDto warehouseResponse = new(warehouse.ID, warehouse.Name, warehouse.Location, warehouse.Capacity, warehouse.Products.Count,warehouse.CreatedAt,warehouse.UpdatedAt);

        await _cache.SetAsync(key, warehouseResponse, TimeSpan.FromMinutes(10));

        return warehouseResponse;
    }

    public async Task UpdateWarehouse(Guid Id, WarehouseRequestDto warehouseRequestDto)
    {
        if(!Validate(warehouseRequestDto))
            throw new Exception("Invalid Request");
        await _warehouseRepository.UpdateWarehouse(new Warehouse
        {
            ID = Id,
            Name = warehouseRequestDto.Name,
            Location = warehouseRequestDto.Location,
            Capacity = warehouseRequestDto.Capacity
        });

        await _cache.RemoveAsync(CacheKeys.WarehouseById(Id));
        await _cache.RemoveByPrefixAsync(CacheKeys.WarehousesPrefix);

    }

    private static bool Validate(WarehouseRequestDto warehouseRequestDto)
    {
        if (string.IsNullOrEmpty(warehouseRequestDto.Name) || string.IsNullOrEmpty(warehouseRequestDto.Location) || warehouseRequestDto.Capacity <= 0)
            return false;
        return true;
    }

    public async Task<PagedResult<WarehouseResponseDto>> GetPagedAsync(WarehouseQueryParams queryParams)
    {
        var key = CacheKeys.WarehousesPaged(queryParams);
        var cached = await _cache.GetAsync<PagedResult<WarehouseResponseDto>>(key);
        if (cached is not null) return cached;

        var paged = await _warehouseRepository.GetPagedAsync(queryParams);

        var mappedItems = paged.Items
            .Select(w => new WarehouseResponseDto(
                Id: w.ID,
                Name: w.Name,
                Location: w.Location,
                Capacity: w.Capacity,
                ProductCount: w.Products?.Count ?? 0,
                CreatedAt: w.CreatedAt,
                UpdatedAt: w.UpdatedAt))
            .ToList();

        var result = new PagedResult<WarehouseResponseDto>
        {
            Items = mappedItems,
            NextCursor = paged.NextCursor
        };
        await _cache.SetAsync(key, result, TimeSpan.FromMinutes(5));
        return result;
    }
}
