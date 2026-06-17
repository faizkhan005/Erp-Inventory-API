namespace ERPInventoryApi.Application.DTOs;

public class CategoryQueryParams
{
    public Guid? Cursor { get; set; }

    private int _pageSize = 10;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 50 ? 50 : value < 1 ? 1 : value;
    }

    public string? Search { get; set; }             // searches Name and Description

    public string SortBy { get; set; } = "createdat";  // name | createdat
    public string SortOrder { get; set; } = "desc";
}
