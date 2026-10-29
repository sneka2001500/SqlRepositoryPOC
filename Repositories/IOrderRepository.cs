using SqlRepositoryPOC.Models;

namespace SqlRepositoryPOC.Repositories;

public interface IOrderRepository
{
    Task<Order> CreateAsync(
        Order order,
        List<OrderProduct> products);

    Task<List<Order>> GetAllAsync();

    Task<List<Order>> GetByEmailAsync(string email);

    Task<Order?> UpdateAsync(
        int orderId,
        string status,
        DateTime deliveryDate);
}