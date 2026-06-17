using ERPInventoryApi.Application.DTOs;
using ERPInventoryApi.Application.Helper;
using ERPInventoryApi.Application.Interfaces;
using ERPInventoryApi.Domain.Entities;

namespace ERPInventoryApi.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICacheService _cache;
    public CategoryService(ICategoryRepository categoryRepository, ICacheService cache)
    {
        _categoryRepository = categoryRepository;
        _cache = cache;
    }
    public async Task AddCategory(CategoryRequestDto categoryRequestDto)
    {
        if (!Validate(categoryRequestDto))
            throw new Exception("Invalid category data.");

        Category category = new()
        {
            Name = categoryRequestDto.Name,
            Description = categoryRequestDto.Description
        };
        await _categoryRepository.CreateNewCategory(category);
        await _cache.RemoveByPrefixAsync(CacheKeys.CategoriesPrefix);
    }

    public async Task DeleteCategory(Guid categoryID)
    {
        await _categoryRepository.DeleteCategory(categoryID);
        await _cache.RemoveAsync(CacheKeys.CategoryById(categoryID));
        await _cache.RemoveByPrefixAsync(CacheKeys.CategoriesPrefix);
    }

    public async Task<List<CategoryResponseDto>> GetAll()
    {
        List<Category> categories = await _categoryRepository.GetAll();
        List<CategoryResponseDto> categoryResponseDtos = [.. categories.Select(c => new CategoryResponseDto(c.ID,c.Name,c.Description,c.Products.Count,c.CreatedAt,c.UpdatedAt))];

        return categoryResponseDtos;
    }

    public async Task<CategoryResponseDto> GetById(Guid categoryID)
    {
        var key = CacheKeys.CategoryById(categoryID);
        var cached = await _cache.GetAsync<CategoryResponseDto>(key);
        if (cached is not null) return cached;

        Category category = await _categoryRepository.GetById(categoryID)?? throw new Exception("Category not found.");
        var result = new CategoryResponseDto(category.ID, category.Name, category.Description, category.Products.Count, category.CreatedAt, category.UpdatedAt);
        await _cache.SetAsync(key, result, TimeSpan.FromMinutes(10));
        return result;
    }

    public async Task UpdateCategory(Guid Id,CategoryRequestDto categoryRequestDto)
    {
        if (!Validate(categoryRequestDto))
            throw new Exception("Invalid category data.");
        Category categoryToUpdate = new()
        {
            ID = Id,
            Name = categoryRequestDto.Name,
            Description = categoryRequestDto.Description
        };
        await _categoryRepository.UpdateCategory(categoryToUpdate);
        await _cache.RemoveAsync(CacheKeys.CategoryById(Id));
        await _cache.RemoveByPrefixAsync(CacheKeys.CategoriesPrefix);
    }

    private static bool Validate(CategoryRequestDto category) 
    {
        if (string.IsNullOrEmpty(category.Name) || string.IsNullOrEmpty(category.Description))
            return false;
        return true;
    }

    public async Task<PagedResult<CategoryResponseDto>> GetPagedAsync(CategoryQueryParams queryParams)
    {

        var key = CacheKeys.CategoriesPaged(queryParams);
        var cached = await _cache.GetAsync<PagedResult<CategoryResponseDto>>(key);
        if (cached is not null) return cached;

        var paged = await _categoryRepository.GetPagedAsync(queryParams);

        var mappedItems = paged.Items
            .Select(c => new CategoryResponseDto(
                Id: c.ID,
                Name: c.Name,
                Description: c.Description,
                ProductCount: c.Products?.Count ?? 0,
                CreatedAt: c.CreatedAt,
                UpdatedAt: c.UpdatedAt))
            .ToList();

        var result = new PagedResult<CategoryResponseDto>
        {
            Items = mappedItems,
            NextCursor = paged.NextCursor
        };

        await _cache.SetAsync(key, result, TimeSpan.FromMinutes(5));

        return result;
    }

}
