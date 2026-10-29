using Microsoft.Data.SqlClient;
using SqlRepositoryPOC.Models;
using System.Data;

namespace SqlRepositoryPOC.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly string _connectionString;

    public ProductRepository(IConfiguration configuration)
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
    // ADD PRODUCT
    public async Task<Product> AddAsync(
        Product product,
        List<string> imageUrls)
    {
        const string productQuery = @"
            INSERT INTO Products
            (
                Name,
                Sku,
                Description,
                ShortDescription,
                Colour,
                ColourCode,
                CategoryId,
                SubCategoryId,
                Price,
                Stock,
                CreatedAt,
                UpdatedAt
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @Name,
                @Sku,
                @Description,
                @ShortDescription,
                @Colour,
                @ColourCode,
                @CategoryId,
                @SubCategoryId,
                @Price,
                @Stock,
                @CreatedAt,
                @UpdatedAt
            )";
await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(productQuery, connection);

        AddProductParameters(command, product);

        var result =
            await command.ExecuteScalarAsync();

        product.Id = Convert.ToInt32(result);

        // Insert images into ProductImages
        foreach (var imageUrl in imageUrls)
        {
            const string imageQuery = @"
                INSERT INTO ProductImages
                (
                    ProductId,
                    ImageUrl
                )
                VALUES
                (
                    @ProductId,
                    @ImageUrl
                )";

            await using var imageCommand =
                new SqlCommand(imageQuery, connection);

            imageCommand.Parameters.AddWithValue(
                "@ProductId",
                product.Id);

            imageCommand.Parameters.AddWithValue(
                "@ImageUrl",
                imageUrl);

            await imageCommand.ExecuteNonQueryAsync();
        }

        return product;
    }

    // GET PRODUCTS BY CATEGORY SLUG
    public async Task<List<Product>> GetByCategorySlugAsync(
        string slug)
    {
        const string query = @"
            SELECT DISTINCT
                p.Id,
                p.Name,
                p.Sku,
                p.Description,
                p.ShortDescription,
                p.Colour,
                p.ColourCode,
                p.CategoryId,
                p.SubCategoryId,
                p.Price,
                p.Stock,
                p.CreatedAt,
                p.UpdatedAt
            FROM Products p
            INNER JOIN Categories c
                ON p.CategoryId = c.Id
                OR p.SubCategoryId = c.Id
            WHERE c.Slug = @Slug";

        var products = new List<Product>();
await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@Slug", slug);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(MapProduct(reader));
        }

        return products;
    }
public async Task<int> BulkInsertAsync(List<Product> products)
{
    if (products == null || products.Count == 0)
        return 0;

    await using var connection = CreateConnection();
    await connection.OpenAsync();

    await using var transaction =
        (SqlTransaction)await connection.BeginTransactionAsync();

    try
    {
        var table = new DataTable();

        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Sku", typeof(string));
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("ShortDescription", typeof(string));
        table.Columns.Add("Colour", typeof(string));
        table.Columns.Add("ColourCode", typeof(string));
        table.Columns.Add("CategoryId", typeof(int));
        table.Columns.Add("SubCategoryId", typeof(int));
        table.Columns.Add("Price", typeof(decimal));
        table.Columns.Add("Stock", typeof(string));
        table.Columns.Add("CreatedAt", typeof(DateTime));
        table.Columns.Add("UpdatedAt", typeof(DateTime));

        foreach (var product in products)
        {
           var now = DateTime.UtcNow;

table.Rows.Add(
    product.Name,
    product.Sku,
    product.Description,
    product.ShortDescription,
    product.Colour,
    product.ColourCode,
    product.CategoryId ?? (object)DBNull.Value,
    product.SubCategoryId ?? (object)DBNull.Value,
    product.Price,
    product.Stock,
    product.CreatedAt == default ? now : product.CreatedAt,
    product.UpdatedAt == default ? now : product.UpdatedAt
);
        }
using var bulkCopy = new SqlBulkCopy(
    connection,
    SqlBulkCopyOptions.CheckConstraints,
    transaction
);

bulkCopy.DestinationTableName = "Products";

bulkCopy.ColumnMappings.Add("Name", "Name");
bulkCopy.ColumnMappings.Add("Sku", "Sku");
bulkCopy.ColumnMappings.Add("Description", "Description");
bulkCopy.ColumnMappings.Add("ShortDescription", "ShortDescription");
bulkCopy.ColumnMappings.Add("Colour", "Colour");
bulkCopy.ColumnMappings.Add("ColourCode", "ColourCode");
bulkCopy.ColumnMappings.Add("CategoryId", "CategoryId");
bulkCopy.ColumnMappings.Add("SubCategoryId", "SubCategoryId");
bulkCopy.ColumnMappings.Add("Price", "Price");
bulkCopy.ColumnMappings.Add("Stock", "Stock");
bulkCopy.ColumnMappings.Add("CreatedAt", "CreatedAt");
bulkCopy.ColumnMappings.Add("UpdatedAt", "UpdatedAt");

        await bulkCopy.WriteToServerAsync(table);

        await transaction.CommitAsync();

        return products.Count;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
    
    
    // GET PRODUCT BY SKU
    public async Task<Product?> GetBySkuAsync(string sku)
    {
        const string query = @"
            SELECT
                Id,
                Name,
                Sku,
                Description,
                ShortDescription,
                Colour,
                ColourCode,
                CategoryId,
                SubCategoryId,
                Price,
                Stock,
                CreatedAt,
                UpdatedAt
            FROM Products
            WHERE Sku = @Sku";

      await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@Sku", sku);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        var product = MapProduct(reader);

        return product;
    }

    // GET ALL PRODUCTS
    public async Task<List<Product>> GetAllAsync()
    {
        const string query = @"
            SELECT
                Id,
                Name,
                Sku,
                Description,
                ShortDescription,
                Colour,
                ColourCode,
                CategoryId,
                SubCategoryId,
                Price,
                Stock,
                CreatedAt,
                UpdatedAt
            FROM Products";

        var products = new List<Product>();

     await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(MapProduct(reader));
        }

        return products;
    }

    // UPDATE PRODUCT
    public async Task<Product?> UpdateAsync(
        Product product,
        List<string> imageUrls)
    {
        const string updateQuery = @"
            UPDATE Products
            SET
                Name = @Name,
                Description = @Description,
                SubCategoryId = @SubCategoryId,
                Price = @Price,
                Stock = @Stock,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

       await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(updateQuery, connection);

        command.Parameters.AddWithValue("@Id", product.Id);
        command.Parameters.AddWithValue("@Name", product.Name);
        command.Parameters.AddWithValue(
            "@Description",
            product.Description);

        command.Parameters.AddWithValue(
            "@SubCategoryId",
            product.SubCategoryId.HasValue
                ? product.SubCategoryId.Value
                : DBNull.Value);

        command.Parameters.AddWithValue(
            "@Price",
            product.Price);

        command.Parameters.AddWithValue(
            "@Stock",
            product.Stock);

        command.Parameters.AddWithValue(
            "@UpdatedAt",
            product.UpdatedAt);

        var rowsAffected =
            await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            return null;
        }

        // Remove old images
        const string deleteImagesQuery = @"
            DELETE FROM ProductImages
            WHERE ProductId = @ProductId";

        await using var deleteImageCommand =
            new SqlCommand(
                deleteImagesQuery,
                connection);

        deleteImageCommand.Parameters.AddWithValue(
            "@ProductId",
            product.Id);

        await deleteImageCommand.ExecuteNonQueryAsync();

        // Insert new images
        foreach (var imageUrl in imageUrls)
        {
            const string imageQuery = @"
                INSERT INTO ProductImages
                (
                    ProductId,
                    ImageUrl
                )
                VALUES
                (
                    @ProductId,
                    @ImageUrl
                )";

            await using var imageCommand =
                new SqlCommand(imageQuery, connection);

            imageCommand.Parameters.AddWithValue(
                "@ProductId",
                product.Id);

            imageCommand.Parameters.AddWithValue(
                "@ImageUrl",
                imageUrl);

            await imageCommand.ExecuteNonQueryAsync();
        }

        return product;
    }

    // DELETE ONE PRODUCT
    public async Task<bool> DeleteAsync(int id)
    {
        const string query = @"
            DELETE FROM Products
            WHERE Id = @Id";

     await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@Id", id);

        var rowsAffected =
            await command.ExecuteNonQueryAsync();

        return rowsAffected > 0;
    }

    // DELETE ALL PRODUCTS
    public async Task<int> DeleteAllAsync()
    {
        const string query = @"
            DELETE FROM Products";
await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        var rowsAffected =
            await command.ExecuteNonQueryAsync();

        return rowsAffected;
    }

    // MAP SQL ROW → PRODUCT
    private static Product MapProduct(
        SqlDataReader reader)
    {
        return new Product
        {
            Id = reader.GetInt32(
                reader.GetOrdinal("Id")),

            Name = reader.GetString(
                reader.GetOrdinal("Name")),

            Sku = reader.GetString(
                reader.GetOrdinal("Sku")),

            Description = reader.GetString(
                reader.GetOrdinal("Description")),

            ShortDescription = reader.GetString(
                reader.GetOrdinal("ShortDescription")),

            Colour = reader.GetString(
                reader.GetOrdinal("Colour")),

            ColourCode = reader.GetString(
                reader.GetOrdinal("ColourCode")),

            CategoryId = reader.IsDBNull(
                reader.GetOrdinal("CategoryId"))
                ? null
                : reader.GetInt32(
                    reader.GetOrdinal("CategoryId")),

            SubCategoryId = reader.IsDBNull(
                reader.GetOrdinal("SubCategoryId"))
                ? null
                : reader.GetInt32(
                    reader.GetOrdinal("SubCategoryId")),

            Price = reader.GetDecimal(
                reader.GetOrdinal("Price")),

            Stock = reader.GetString(
                reader.GetOrdinal("Stock")),

            CreatedAt = reader.GetDateTime(
                reader.GetOrdinal("CreatedAt")),

            UpdatedAt = reader.GetDateTime(
                reader.GetOrdinal("UpdatedAt"))
        };
    }
public async Task<List<string>> GetImagesAsync(int productId)
{
    const string query = @"
        SELECT ImageUrl
        FROM ProductImages
        WHERE ProductId = @ProductId";

    var images = new List<string>();

  await using var connection = CreateConnection();

    await connection.OpenAsync();

    await using var command =
        new SqlCommand(query, connection);

    command.Parameters.AddWithValue(
        "@ProductId",
        productId);

    await using var reader =
        await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        images.Add(
            reader.GetString(
                reader.GetOrdinal("ImageUrl")));
    }

    return images;
}
public async Task<Product?> GetByIdAsync(int id)
{
    const string query = @"
        SELECT
            Id,
            Name,
            Sku,
            Description,
            ShortDescription,
            Colour,
            ColourCode,
            CategoryId,
            SubCategoryId,
            Price,
            Stock,
            CreatedAt,
            UpdatedAt
        FROM Products
        WHERE Id = @Id";

  await using var connection = CreateConnection();

    await connection.OpenAsync();

    await using var command =
        new SqlCommand(query, connection);

    command.Parameters.AddWithValue("@Id", id);

    await using var reader =
        await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
        return null;

    return MapProduct(reader);
}
    // ADD PRODUCT PARAMETERS
    private static void AddProductParameters(
        SqlCommand command,
        Product product)
    {
        command.Parameters.AddWithValue(
            "@Name",
            product.Name);

        command.Parameters.AddWithValue(
            "@Sku",
            product.Sku);

        command.Parameters.AddWithValue(
            "@Description",
            product.Description);

        command.Parameters.AddWithValue(
            "@ShortDescription",
            product.ShortDescription);

        command.Parameters.AddWithValue(
            "@Colour",
            product.Colour);

        command.Parameters.AddWithValue(
            "@ColourCode",
            product.ColourCode);

        command.Parameters.AddWithValue(
            "@CategoryId",
            product.CategoryId.HasValue
                ? product.CategoryId.Value
                : DBNull.Value);

        command.Parameters.AddWithValue(
            "@SubCategoryId",
            product.SubCategoryId.HasValue
                ? product.SubCategoryId.Value
                : DBNull.Value);

        command.Parameters.AddWithValue(
            "@Price",
            product.Price);

        command.Parameters.AddWithValue(
            "@Stock",
            product.Stock);

        command.Parameters.AddWithValue(
            "@CreatedAt",
            product.CreatedAt);

        command.Parameters.AddWithValue(
            "@UpdatedAt",
            product.UpdatedAt);
    }
}