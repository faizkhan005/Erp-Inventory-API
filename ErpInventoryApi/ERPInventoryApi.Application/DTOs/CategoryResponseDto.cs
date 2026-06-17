namespace ERPInventoryApi.Application.DTOs;

public record CategoryResponseDto(
    Guid Id,
    string Name,
    string Description,
    int ProductCount,       // how many products are in this category
    DateTime CreatedAt,
    DateTime UpdatedAt
);