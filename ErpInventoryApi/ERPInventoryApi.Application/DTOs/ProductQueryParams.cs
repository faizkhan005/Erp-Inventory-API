namespace ERPInventoryApi.Application.DTOs;

public class ProductQueryParams
{
    // Cursor pagination 
    /// <summary>
    /// The ID of the last item from the previous page.
    /// Null means start from the beginning.
    /// </summary>
    public Guid? Cursor { get; set; }

    private int _pageSize = 10;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 50 ? 50 : value < 1 ? 1 : value; // clamp 1-50
    }

    // Filtering 
    public Guid? CategoryId { get; set; }
    public Guid? WarehouseId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    /// <summary>Searches Name, SKU, and Description.</summary>
    public string? Search { get; set; }

    // Sorting 
    /// <summary>Field to sort by: name, price, stockquantity, createdat</summary>
    public string SortBy { get; set; } = "createdat";
    public string SortOrder { get; set; } = "desc"; // "asc" | "desc"
}

