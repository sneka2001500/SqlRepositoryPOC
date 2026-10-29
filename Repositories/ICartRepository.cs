using SqlRepositoryPOC.Models;

namespace SqlRepositoryPOC.Repositories;

public interface ICartRepository
{
    Task<List<Cart>> GetAllAsync();

    Task<Cart?> GetByCustomerIdAsync(int customerId);

    Task<Cart> AddToCartAsync(
        int customerId,
        List<CartProduct> products);

    Task<Cart?> RemoveFromCartAsync(
        int customerId,
        int productId);

    Task<bool> ClearCartAsync(int customerId);

    Task<bool> DeleteByCustomerIdAsync(int customerId);

    Task<bool> DeleteAllAsync();
}