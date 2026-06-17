using ERPInventoryApi.Application.DTOs;

namespace ERPInventoryApi.Application.Helper;


/// <summary>
/// Centralizes all cache key generation.
/// Consistent keys prevent bugs where Set uses a different key than Get.
/// </summary>
public static class CacheKeys
{
    // Products
    public const string ProductsPrefix = "products:";

    public static string ProductById(Guid id)
        => $"products:single:{id}";

    public static string ProductsPaged(ProductQueryParams q)
        => $"products:paged:cursor={q.Cursor}:size={q.PageSize}:cat={q.CategoryId}:" +
           $"wh={q.WarehouseId}:min={q.MinPrice}:max={q.MaxPrice}:" +
           $"search={q.Search}:sortBy={q.SortBy}:sortOrder={q.SortOrder}";

    // Categories
    public const string CategoriesPrefix = "categories:";

    public static string CategoryById(Guid id)
        => $"categories:single:{id}";

    public static string CategoriesPaged(CategoryQueryParams q)
        => $"categories:paged:cursor={q.Cursor}:size={q.PageSize}:" +
           $"search={q.Search}:sortBy={q.SortBy}:sortOrder={q.SortOrder}";

    // Warehouses
    public const string WarehousesPrefix = "warehouses:";

    public static string WarehouseById(Guid id)
        => $"warehouses:single:{id}";

    public static string WarehousesPaged(WarehouseQueryParams q)
        => $"warehouses:paged:cursor={q.Cursor}:size={q.PageSize}:" +
           $"search={q.Search}:min={q.MinCapacity}:max={q.MaxCapacity}:" +
           $"sortBy={q.SortBy}:sortOrder={q.SortOrder}";
}
