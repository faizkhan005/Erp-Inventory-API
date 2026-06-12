using ERPInventoryApi.Application.DTOs;
using ERPInventoryApi.Application.Interfaces;
using ERPInventoryApi.Domain.Entities;

namespace ERPInventoryApi.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
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
    }

    public async Task DeleteCategory(Guid categoryID)
    {
        await _categoryRepository.DeleteCategory(categoryID);
    }

    public async Task<List<CategoryResponseDto>> GetAll()
    {
        List<Category> categories = await _categoryRepository.GetAll();
        List<CategoryResponseDto> categoryResponseDtos = [.. categories.Select(c => new CategoryResponseDto(c.ID,c.Name,c.Description))];

        return categoryResponseDtos;
    }

    public async Task<CategoryResponseDto> GetById(Guid categoryID)
    {
        Category category = await _categoryRepository.GetById(categoryID)?? throw new Exception("Category not found.");

        return new CategoryResponseDto(category.ID, category.Name, category.Description);
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
    }

    private static bool Validate(CategoryRequestDto category) 
    {
        if (string.IsNullOrEmpty(category.Name) || string.IsNullOrEmpty(category.Description))
            return false;
        return true;
    }
}
