using ERPInventoryApi.Application.DTOs;
using ERPInventoryApi.Application.Interfaces;
using ERPInventoryApi.Domain.Entities;

namespace ERPInventoryApi.Application.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;

    public WarehouseService(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
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
    }

    public async Task DeleteWarehouse(Guid warehouseID)
    {
        await _warehouseRepository.DeleteById(warehouseID);
    }

    public async Task<List<WarehouseResponseDto>> GetAll()
    {
        List<Warehouse> warehouses = await _warehouseRepository.GetAllWarehouse();
        List<WarehouseResponseDto> listWarehouse = [.. warehouses.Select(w => new WarehouseResponseDto(w.ID, w.Name, w.Location, w.Capacity, w.Products.Count,w.CreatedAt,w.UpdatedAt))];
        return listWarehouse;
    }

    public async Task<WarehouseResponseDto> GetById(Guid warehouseID)
    {
        Warehouse warehouse = await _warehouseRepository.GetById(warehouseID) ?? throw new Exception("Warehouse Not Found");
        WarehouseResponseDto warehouseResponse = new(warehouse.ID, warehouse.Name, warehouse.Location, warehouse.Capacity, warehouse.Products.Count,warehouse.CreatedAt,warehouse.UpdatedAt);
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

    }

    private static bool Validate(WarehouseRequestDto warehouseRequestDto)
    {
        if (string.IsNullOrEmpty(warehouseRequestDto.Name) || string.IsNullOrEmpty(warehouseRequestDto.Location) || warehouseRequestDto.Capacity <= 0)
            return false;
        return true;
    }

    public async Task<PagedResult<WarehouseResponseDto>> GetPagedAsync(WarehouseQueryParams queryParams)
    {
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

        return new PagedResult<WarehouseResponseDto>
        {
            Items = mappedItems,
            NextCursor = paged.NextCursor
        };
    }
}
