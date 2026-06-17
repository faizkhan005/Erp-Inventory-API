using ERPInventoryApi.Application.DTOs;
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

    public async Task<PagedResult<Category>> GetPagedAsync(CategoryQueryParams q)
    {
        var query = _dbContext.Categories
            .Include(c => c.Products)   // for ProductCount
            .AsQueryable();

        // Filtering 
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var search = q.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(search) ||
                c.Description.ToLower().Contains(search));
        }

        // Sorting 
        var isAsc = q.SortOrder.ToLower() == "asc";

        IOrderedQueryable<Category> orderedQuery = q.SortBy.ToLower() switch
        {
            "name" => isAsc ? query.OrderBy(c => c.Name)
                            : query.OrderByDescending(c => c.Name),
            _ => isAsc ? query.OrderBy(c => c.CreatedAt)
                            : query.OrderByDescending(c => c.CreatedAt)
        };

        orderedQuery = orderedQuery.ThenBy(c => c.ID);

        query = orderedQuery;

        // Cursor
        if (q.Cursor.HasValue)
        {
            var cursorItem = await _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ID == q.Cursor.Value);

            if (cursorItem != null)
            {
                query = q.SortBy.ToLower() switch
                {
                    "name" => isAsc
                        ? query.Where(c => c.Name.CompareTo(cursorItem.Name) > 0
                            || (c.Name == cursorItem.Name && c.ID > cursorItem.ID))
                        : query.Where(c => c.Name.CompareTo(cursorItem.Name) < 0
                            || (c.Name == cursorItem.Name && c.ID > cursorItem.ID)),

                    _ => isAsc
                        ? query.Where(c => c.CreatedAt > cursorItem.CreatedAt
                            || (c.CreatedAt == cursorItem.CreatedAt && c.ID > cursorItem.ID))
                        : query.Where(c => c.CreatedAt < cursorItem.CreatedAt
                            || (c.CreatedAt == cursorItem.CreatedAt && c.ID > cursorItem.ID))
                };
            }
        }

        // Fetch + detect next page
        var items = await query
            .AsNoTracking()
            .Take(q.PageSize + 1)
            .ToListAsync();

        var hasNextPage = items.Count > q.PageSize;
        if (hasNextPage) items.RemoveAt(items.Count - 1);

        return new PagedResult<Category>
        {
            Items = items,
            NextCursor = hasNextPage ? items[^1].ID : null
        };
    }
}
