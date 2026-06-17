using ERPInventoryApi.Application.DTOs;
using ERPInventoryApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPInventoryApi.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehouseController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllWareHouse() 
    {
        List<WarehouseResponseDto> response = await _warehouseService.GetAll();
        return CreatedAtAction(nameof(GetAllWareHouse), response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetWarehouseById(Guid Id) 
    {
        WarehouseResponseDto response = await _warehouseService.GetById(Id);
        return CreatedAtAction(nameof(GetWarehouseById), response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWarehouse([FromBody] WarehouseRequestDto request) 
    {
        await _warehouseService.AddWarehouse(request);
        return CreatedAtAction(nameof(CreateWarehouse), new { success = "Successfully Created new Warehouse" }, request);
    }

    [HttpPut("{Id}")]
    public async Task<IActionResult> UpdateWareHouse(Guid Id, [FromBody] WarehouseRequestDto request) 
    {
        await _warehouseService.UpdateWarehouse(Id, request);
        return CreatedAtAction(nameof(UpdateWareHouse), new { success = $"Successfully Updated Warehouse {Id}" }, request);
    }

    [HttpGet("filter")]
    [ProducesResponseType(typeof(PagedResult<WarehouseResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] WarehouseQueryParams queryParams)
    {
        var result = await _warehouseService.GetPagedAsync(queryParams);
        return Ok(result);
    }

    [HttpDelete("{Id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteWarehouse(Guid Id)
    {
        await _warehouseService.DeleteWarehouse(Id);
        return CreatedAtAction(nameof(DeleteWarehouse), new { success = $"Successfully Deleted Warehouse {Id}" }, Id);
    }
}
