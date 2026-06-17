using ERPInventoryApi.Application.DTOs;
using ERPInventoryApi.Application.Helper;
using ERPInventoryApi.Application.Interfaces;
using ERPInventoryApi.Domain.Entities;

namespace ERPInventoryApi.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cache;
    public ProductService(IProductRepository productRepository,
        ICacheService cache)
    {
        _productRepository = productRepository;
        _cache = cache;
    }
    public async Task AddProduct(ProductRequestDto productRequestDto)
    {
        if (!Validate(productRequestDto))
            throw new ArgumentException("Invalid Product Data");
        Product newProd = RequestToProduct(productRequestDto, Guid.Empty);
        await _productRepository.CreateNewProduct(newProd);
        await _cache.RemoveByPrefixAsync(CacheKeys.ProductsPrefix);
    }

    public async Task DeleteProduct(Guid productID)
    {
        await _productRepository.DeleteById(productID);
        await _cache.RemoveAsync(CacheKeys.ProductById(productID));
        await _cache.RemoveByPrefixAsync(CacheKeys.ProductsPrefix);
    }

    public async Task<List<ProductResponseDto>> GetAll()
    {
        List<Product> products = await _productRepository.GetAll();
        List<ProductResponseDto> productResponseDtos = [.. products.Select(p => new ProductResponseDto(p.ID, p.Name,p.SKU, p.Description, p.Price, p.StockQuantity, p.ReorderPoint, p.CategoryId, p.Category.Name, p.WarehouseId, p.Warehouse.Name, p.CreatedAt, p.UpdatedAt))];
        return productResponseDtos;
    }

    public async Task<ProductResponseDto> GetById(Guid productID)
    {
        var key = CacheKeys.ProductById(productID);
        var cached = await _cache.GetAsync<ProductResponseDto>(key);
        if (cached is not null) return cached;

        var product = await _productRepository.GetById(productID)??throw new Exception("Product not found");

        var result = MapToDto(product);
        await _cache.SetAsync(key, result, TimeSpan.FromMinutes(10));
        return result;
    }

    public async Task UpdateProduct(Guid Id, ProductRequestDto productRequestDto)
    {
        if(!Validate(productRequestDto))
            throw new Exception("Invalid product data.");
        Product productToUpdate = RequestToProduct(productRequestDto, Id);

        await _productRepository.UpdateProduct(productToUpdate);
        // Invalidate specific item + all paged results
        await _cache.RemoveAsync(CacheKeys.ProductById(Id));
        await _cache.RemoveByPrefixAsync(CacheKeys.ProductsPrefix);
    }

    private static bool Validate(ProductRequestDto productRequestDto)
    {
        if (string.IsNullOrEmpty(productRequestDto.Name) || string.IsNullOrEmpty(productRequestDto.SKU) || productRequestDto.Price <= 0 || productRequestDto.StockQuantity < 0 || productRequestDto.ReorderPoint < 0 ||productRequestDto.CategoryId == Guid.Empty || productRequestDto.WarehouseId == Guid.Empty)
            return false;
        return true;
    }

    private static Product RequestToProduct(ProductRequestDto productRequestDto, Guid Id) 
    {
        Product product = new()
        {
            Name = productRequestDto.Name,
            SKU = productRequestDto.SKU,
            Description = productRequestDto.Description,
            Price = productRequestDto.Price,
            StockQuantity = productRequestDto.StockQuantity,
            ReorderPoint = productRequestDto.ReorderPoint,
            CategoryId = productRequestDto.CategoryId,
            WarehouseId = productRequestDto.WarehouseId
        };
        if(Id != Guid.Empty)
            product.ID = Id;
        return product;
    }

    public async Task<PagedResult<ProductResponseDto>> GetPagedAsync(ProductQueryParams queryParams)
    {
        var key = CacheKeys.ProductsPaged(queryParams);
        var cached = await _cache.GetAsync<PagedResult<ProductResponseDto>>(key);
        if (cached is not null) return cached;

        var paged = await _productRepository.GetPagedAsync(queryParams);

        var result = MapToPagedResult(paged);

        await _cache.SetAsync(key, result, TimeSpan.FromMinutes(5));
        return result;
    }

    // Mapping helpers
    private static ProductResponseDto MapToDto(Product p) => new(
        Id: p.ID,
        Name: p.Name,
        SKU: p.SKU,
        Description: p.Description,
        Price: p.Price,
        StockQuantity: p.StockQuantity,
        ReorderPoint: p.ReorderPoint,
        CategoryId: p.CategoryId,
        CategoryName: p.Category?.Name ?? string.Empty,
        WarehouseId: p.WarehouseId,
        WarehouseName: p.Warehouse?.Name ?? string.Empty,
        CreatedAt: p.CreatedAt,
        UpdatedAt: p.UpdatedAt);

    private static PagedResult<ProductResponseDto> MapToPagedResult(PagedResult<Product> paged) => new()
    {
        Items = [.. paged.Items.Select(MapToDto)],
        NextCursor = paged.NextCursor
    };
}
