namespace ERPInventoryApi.Application.DTOs;

public record WarehouseResponseDto(
    Guid Id,
    string Name,
    string Location,
    int Capacity,
    int ProductCount,       // how many products are stored here
    DateTime CreatedAt,
    DateTime UpdatedAt
);
