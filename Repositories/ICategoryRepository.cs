using SqlRepositoryPOC.Models;

namespace SqlRepositoryPOC.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id);

    Task<Category?> GetByNameAsync(string name);

    Task<List<Category>> GetAllAsync();

    Task<Category> AddAsync(Category category);
}