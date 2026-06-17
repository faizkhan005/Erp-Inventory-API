namespace ERPInventoryApi.Application.DTOs;

public record ProductResponseDto(
    Guid Id,
    string Name,
    string SKU,
    string Description,
    decimal Price,
    int StockQuantity,
    int ReorderPoint,
    Guid CategoryId,
    string CategoryName,
    Guid WarehouseId,
    string WarehouseName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
