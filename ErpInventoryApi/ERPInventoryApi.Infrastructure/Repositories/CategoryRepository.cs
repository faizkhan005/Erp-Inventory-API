using ERPInventoryApi.Application.Interfaces;
using ERPInventoryApi.Domain.Entities;
using ERPInventoryApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERPInventoryApi.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _dbContext;

    public CategoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task CreateNewCategory(Category category)
    {
        await _dbContext.AddAsync(category);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteCategory(Guid categoryID)
    {
        Category category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.ID == categoryID)?? throw new Exception("Entity Not Found");
        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    public Task<List<Category>> GetAll()
    {
        return _dbContext.Categories.ToListAsync();
    }

    public async Task<Category> GetById(Guid categoryID) 
    {
        return await _dbContext.Categories.FirstOrDefaultAsync(c => c.ID == categoryID) ?? throw new Exception("Entity Not Found");
    }

    public async Task UpdateCategory(Category category)
    {
        Category existingCategory = await _dbContext.Categories.FirstOrDefaultAsync(c => c.ID == category.ID) ?? throw new Exception("Entity Not Found");
        existingCategory.Name = category.Name ?? existingCategory.Name;
        existingCategory.Description = category.Description ?? existingCategory.Description;
        existingCategory.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }
}
