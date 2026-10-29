using SqlRepositoryPOC.Models;

namespace SqlRepositoryPOC.Repositories;

public interface IProductRepository
{
    Task<Product> AddAsync(
        Product product,
        List<string> imageUrls);

    Task<Product?> GetByIdAsync(int id);

    Task<List<Product>> GetByCategorySlugAsync(
        string slug);

    Task<Product?> GetBySkuAsync(
        string sku);

    Task<List<Product>> GetAllAsync();

    Task<Product?> UpdateAsync(
        Product product,
        List<string> imageUrls);

    Task<bool> DeleteAsync(int id);

    Task<int> DeleteAllAsync();

    Task<List<string>> GetImagesAsync(
        int productId);

        Task<int> BulkInsertAsync(List<Product> products);
}