namespace ERPInventoryApi.Application.DTOs;

public record WarehouseResponseDto(Guid Id, string Name, string Location, int Capacity);
