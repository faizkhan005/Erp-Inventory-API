using ERPInventoryApi.Domain.Common;

namespace ERPInventoryApi.Domain.Entities;

public class    Warehouse : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public int Capacity { get; set; }

    // Navigation properties
    public ICollection<Product> Products { get; set; } = [];
}
