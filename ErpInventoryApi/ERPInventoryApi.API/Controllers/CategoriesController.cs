using ERPInventoryApi.Application.DTOs;
using ERPInventoryApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPInventoryApi.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }
    [HttpGet]
    public async Task<IActionResult> GetAllCategories()
    {
        List<CategoryResponseDto> response = await _categoryService.GetAll();
        return CreatedAtAction(nameof(GetAllCategories), response);
    }

    [HttpGet("{categoryID}")]
    public async Task<IActionResult> GetCategoryById(Guid categoryID)
    {
        CategoryResponseDto response = await _categoryService.GetById(categoryID);
        return CreatedAtAction(nameof(GetCategoryById), new { categoryID }, response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryRequestDto categoryRequestDto)
    {
        await _categoryService.AddCategory(categoryRequestDto);
        return CreatedAtAction(nameof(CreateCategory), new { success = "Successfully Created new Category" }, categoryRequestDto);
    }

    [HttpPut("{categoryID}")]
    public async Task<IActionResult> UpdateCategory(Guid categoryID, [FromBody] CategoryRequestDto categoryRequestDto)
    {
        await _categoryService.UpdateCategory(categoryID, categoryRequestDto);
        return CreatedAtAction(nameof(UpdateCategory), new { success = $"Successfully Updated Category {categoryID}" }, categoryRequestDto);
    }

    [HttpDelete("{categoryID}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(Guid categoryID)
    {
        await _categoryService.DeleteCategory(categoryID);
        return CreatedAtAction(nameof(DeleteCategory), new { success = $"Successfully Deleted Category {categoryID}" }, categoryID);
    }
}
