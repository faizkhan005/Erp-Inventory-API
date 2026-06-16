using ERPInventoryApi.Application.DTOs;

namespace ERPInventoryApi.Application.Interfaces;

public interface IProductService
{
    Task<ProductResponseDto> GetById(Guid productID);

    Task<List<ProductResponseDto>> GetAll();

    Task AddProduct(ProductRequestDto productRequestDto);

    Task UpdateProduct(Guid Id, ProductRequestDto productRequestDto);

    Task DeleteProduct(Guid productID);

    Task<PagedResult<ProductResponseDto>> GetPagedAsync(ProductQueryParams queryParams);
}
