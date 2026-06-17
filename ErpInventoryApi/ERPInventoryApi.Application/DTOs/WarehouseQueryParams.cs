using System;
using System.Collections.Generic;
using System.Text;

namespace ERPInventoryApi.Application.DTOs;

public class WarehouseQueryParams
{
    public Guid? Cursor { get; set; }

    private int _pageSize = 10;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 50 ? 50 : value < 1 ? 1 : value;
    }

    public string? Search { get; set; }             // searches Name and Location
    public int? MinCapacity { get; set; }
    public int? MaxCapacity { get; set; }

    public string SortBy { get; set; } = "createdat";  // name | capacity | createdat
    public string SortOrder { get; set; } = "desc";
}
