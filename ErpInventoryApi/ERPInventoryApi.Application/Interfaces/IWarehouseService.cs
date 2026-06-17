using ERPInventoryApi.Application.DTOs;

namespace ERPInventoryApi.Application.Interfaces;

public interface IWarehouseService
{
    Task<WarehouseResponseDto> GetById(Guid warehouseID);

    Task<List<WarehouseResponseDto>> GetAll();

    Task AddWarehouse(WarehouseRequestDto warehouseRequestDto);

    Task UpdateWarehouse(Guid Id, WarehouseRequestDto warehouseRequestDto);

    Task DeleteWarehouse(Guid warehouseID);

    Task<PagedResult<WarehouseResponseDto>> GetPagedAsync(WarehouseQueryParams queryParams);
}
