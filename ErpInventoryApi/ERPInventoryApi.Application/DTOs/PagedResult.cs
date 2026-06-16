namespace ERPInventoryApi.Application.DTOs;

public class PagedResult<T>
{
    /// <summary>The actual data for this page.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>
    /// The cursor to pass in the next request to get the next page.
    /// Null means this is the last page.
    /// </summary>
    public Guid? NextCursor { get; init; }

    /// <summary>Whether there are more pages after this one.</summary>
    public bool HasNextPage => NextCursor.HasValue;

    /// <summary>Number of items in this page.</summary>
    public int Count => Items.Count;
}
