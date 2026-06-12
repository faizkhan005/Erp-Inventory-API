using ERPInventoryApi.Application.DTOs;
using ERPInventoryApi.Application.Interfaces;
using ERPInventoryApi.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERPInventoryApi.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllProducts()
    {
        List<ProductResponseDto> response = await _productService.GetAll();
        return CreatedAtAction(nameof(GetAllProducts), response);
    }

    [HttpGet]
    public async Task<IActionResult> GetProductById(Guid productID)
    {
        ProductResponseDto response = await _productService.GetById(productID);
        return CreatedAtAction(nameof(GetProductById), new { productID }, response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] ProductRequestDto productRequestDto)
    {
        await _productService.AddProduct(productRequestDto);
        return CreatedAtAction(nameof(CreateProduct), new { success = "Successfully Created new Product" }, productRequestDto);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProduct(Guid productID, [FromBody] ProductRequestDto productRequestDto)
    {
        await _productService.UpdateProduct(productID, productRequestDto);
        return CreatedAtAction(nameof(UpdateProduct), new { success = $"Successfully Updated Product {productID}" }, productRequestDto);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteProduct(Guid productID)
    {
        await _productService.DeleteProduct(productID);
        return CreatedAtAction(nameof(DeleteProduct), new { success = $"Successfully Deleted Product {productID}" }, productID);
    }

}
