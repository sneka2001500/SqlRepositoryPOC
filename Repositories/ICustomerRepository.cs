using SqlRepositoryPOC.Models;

namespace SqlRepositoryPOC.Repositories;

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllAsync();

    Task<List<Customer>> GetPagedAsync(
    int pageNumber,
    int pageSize
);


Task<List<Customer>> GetFilteredAndSortedAsync(
    string? name,
    string? email,
    string? sortBy,
    string? sortDirection,
    int pageNumber,
    int pageSize);


    Task<Customer?> GetByIdAsync(int id);

    Task<Customer?> GetByEmailAsync(string email);

    Task<Customer> CreateAsync(Customer customer);

    Task<bool> UpdateAsync(Customer customer);

    Task<bool> DeleteByEmailAsync(string email);

    Task<bool> DeleteAllAsync();
    Task<bool> TransactionTestAsync(int customerId);
}

