using ERPInventoryApi.Domain.Entities;

namespace ERPInventoryApi.Application.Interfaces;

public interface IProductRepository
{
    Task CreateNewProduct(Product newProd);

    Task UpdateProduct(Product newProduct);

    Task<Product?> GetById(Guid id);

    Task<List<Product>> GetAll();

    Task DeleteById(Guid id);
}
