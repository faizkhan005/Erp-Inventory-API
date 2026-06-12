namespace ERPInventoryApi.Application.DTOs;

public record ProductRequestDto(string Name, string SKU, string Description, decimal Price, int StockQuantity, int ReorderPoint, Guid CategoryId, Guid WarehouseId);

