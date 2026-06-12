using ERPInventoryApi.Application.DTOs;

namespace ERPInventoryApi.Application.Interfaces;

public interface ICategoryService
{
    Task<CategoryResponseDto> GetById(Guid categoryID);

    Task<List<CategoryResponseDto>> GetAll();

    Task AddCategory(CategoryRequestDto categoryRequestDto);

    Task UpdateCategory(Guid Id, CategoryRequestDto categoryRequestDto);

    Task DeleteCategory(Guid categoryID);
}
