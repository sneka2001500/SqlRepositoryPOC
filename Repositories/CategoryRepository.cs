using Microsoft.Data.SqlClient;
using SqlRepositoryPOC.Models;

namespace SqlRepositoryPOC.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly string _connectionString;

    public CategoryRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found.");
    }
private SqlConnection CreateConnection()
{
    return new SqlConnection(_connectionString);
}
    // GET CATEGORY BY ID
    public async Task<Category?> GetByIdAsync(int id)
    {
        const string query = @"
            SELECT
                Id,
                Name,
                Slug,
                Description,
                ParentId,
                CreatedAt,
                UpdatedAt
            FROM Categories
            WHERE Id = @Id";

      await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@Id", id);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapCategory(reader);
    }

    // GET ALL CATEGORIES
    public async Task<List<Category>> GetAllAsync()
    {
        const string query = @"
            SELECT
                Id,
                Name,
                Slug,
                Description,
                ParentId,
                CreatedAt,
                UpdatedAt
            FROM Categories";

        var categories = new List<Category>();

       await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            categories.Add(MapCategory(reader));
        }

        return categories;
    }

    // ADD CATEGORY
    public async Task<Category> AddAsync(Category category)
    {
        const string query = @"
            INSERT INTO Categories
            (
                Name,
                Slug,
                Description,
                ParentId,
                CreatedAt,
                UpdatedAt
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @Name,
                @Slug,
                @Description,
                @ParentId,
                @CreatedAt,
                @UpdatedAt
            )";

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@Name",
            category.Name);

        command.Parameters.AddWithValue(
            "@Slug",
            category.Slug);

        command.Parameters.AddWithValue(
            "@Description",
            category.Description);

        command.Parameters.AddWithValue(
            "@ParentId",
            category.ParentId.HasValue
                ? category.ParentId.Value
                : DBNull.Value);

        command.Parameters.AddWithValue(
            "@CreatedAt",
            category.CreatedAt);

        command.Parameters.AddWithValue(
            "@UpdatedAt",
            category.UpdatedAt);

        var result =
            await command.ExecuteScalarAsync();

        category.Id = Convert.ToInt32(result);

        return category;
    }

public async Task<Category?> GetByNameAsync(string name)
{
    const string query = @"
        SELECT
            Id,
            Name,
            Slug,
            Description,
            ParentId,
            CreatedAt,
            UpdatedAt
        FROM Categories
        WHERE Name = @Name";

    await using var connection = CreateConnection();

    await connection.OpenAsync();

    await using var command =
        new SqlCommand(query, connection);

    command.Parameters.AddWithValue(
        "@Name",
        name);

    await using var reader =
        await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
        return null;

    return MapCategory(reader);
}
    // CONVERT SQL ROW → CATEGORY OBJECT
    private static Category MapCategory(SqlDataReader reader)
    {
        return new Category
        {
            Id = reader.GetInt32(
                reader.GetOrdinal("Id")),

            Name = reader.GetString(
                reader.GetOrdinal("Name")),

            Slug = reader.GetString(
                reader.GetOrdinal("Slug")),

            Description = reader.GetString(
                reader.GetOrdinal("Description")),

            ParentId = reader.IsDBNull(
                reader.GetOrdinal("ParentId"))
                ? null
                : reader.GetInt32(
                    reader.GetOrdinal("ParentId")),

            CreatedAt = reader.GetDateTime(
                reader.GetOrdinal("CreatedAt")),

            UpdatedAt = reader.GetDateTime(
                reader.GetOrdinal("UpdatedAt"))
        };
    }
}
