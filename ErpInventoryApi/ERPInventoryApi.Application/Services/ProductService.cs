using ERPInventoryApi.Application.DTOs;
using ERPInventoryApi.Application.Interfaces;
using ERPInventoryApi.Domain.Entities;

namespace ERPInventoryApi.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }
    public Task AddProduct(ProductRequestDto productRequestDto)
    {
        if (!Validate(productRequestDto))
            throw new ArgumentException("Invalid Product Data");
        Product newProd = RequestToProduct(productRequestDto, Guid.Empty);
        return _productRepository.CreateNewProduct(newProd);
    }

    public async Task DeleteProduct(Guid productID)
    {
        await _productRepository.DeleteById(productID);
    }

    public async Task<List<ProductResponseDto>> GetAll()
    {
        List<Product> products = await _productRepository.GetAll();
        List<ProductResponseDto> productResponseDtos = [.. products.Select(p => new ProductResponseDto(p.ID, p.Name,p.SKU, p.Description, p.Price, p.StockQuantity, p.ReorderPoint, p.CategoryId, p.Category.Name, p.WarehouseId, p.Warehouse.Name, p.CreatedAt, p.UpdatedAt))];
        return productResponseDtos;
    }

    public async Task<ProductResponseDto> GetById(Guid productID)
    {
        Product product = await _productRepository.GetById(productID) ?? throw new InvalidOperationException("Product not found");
        return new ProductResponseDto(product.ID, product.Name, product.SKU, product.Description, product.Price, product.StockQuantity, product.ReorderPoint, product.CategoryId, product.Category.Name, product.WarehouseId, product.Warehouse.Name, product.CreatedAt, product.UpdatedAt);
    }

    public Task UpdateProduct(Guid Id, ProductRequestDto productRequestDto)
    {
        if(!Validate(productRequestDto))
            throw new Exception("Invalid product data.");
        Product productToUpdate = RequestToProduct(productRequestDto, Id);
        return _productRepository.UpdateProduct(productToUpdate);
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
        var paged = await _productRepository.GetPagedAsync(queryParams);

        var mappedItems = paged.Items
            .Select(p => new ProductResponseDto(
                Id: p.ID,
                Name: p.Name,
                SKU: p.SKU,
                Description: p.Description,
                Price: p.Price,
                StockQuantity: p.StockQuantity,
                ReorderPoint: p.ReorderPoint,
                CategoryId: p.CategoryId,
                CategoryName: p.Category.Name,
                WarehouseId: p.WarehouseId,
                WarehouseName: p.Warehouse.Name,
                CreatedAt: p.CreatedAt,
                UpdatedAt: p.UpdatedAt))
            .ToList();

        return new PagedResult<ProductResponseDto>
        {
            Items = mappedItems,
            NextCursor = paged.NextCursor
        };
    }
}
