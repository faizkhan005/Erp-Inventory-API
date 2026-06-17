using ERPInventoryApi.Application.DTOs;
using ERPInventoryApi.Domain.Entities;

namespace ERPInventoryApi.Application.Interfaces;

public interface ICategoryRepository
{
    Task CreateNewCategory(Category category);

    Task UpdateCategory(Category category);

    Task DeleteCategory(Guid categoryID);

    Task<List<Category>> GetAll();

    Task<Category> GetById(Guid categoryID);

    Task<PagedResult<Category>> GetPagedAsync(CategoryQueryParams queryParams);
}
