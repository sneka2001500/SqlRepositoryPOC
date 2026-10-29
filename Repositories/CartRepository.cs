using Microsoft.Data.SqlClient;
using SqlRepositoryPOC.Models;

namespace SqlRepositoryPOC.Repositories;

public class CartRepository : ICartRepository
{
    private readonly string _connectionString;

    public CartRepository(IConfiguration configuration)
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
    public async Task<List<Cart>> GetAllAsync()
    {
        const string query = @"
            SELECT
                Id,
                CustomerId,
                TotalProductCount,
                TotalPrice,
                CreatedAt,
                UpdatedAt
            FROM Carts";

        var carts = new List<Cart>();

     await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            carts.Add(MapCart(reader));
        }

        return carts;
    }

    public async Task<Cart?> GetByCustomerIdAsync(int customerId)
    {
        const string query = @"
            SELECT
                Id,
                CustomerId,
                TotalProductCount,
                TotalPrice,
                CreatedAt,
                UpdatedAt
            FROM Carts
            WHERE CustomerId = @CustomerId";

     await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@CustomerId",
            customerId);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return MapCart(reader);
    }

    public async Task<Cart> AddToCartAsync(
        int customerId,
        List<CartProduct> products)
    {
        await using var connection = CreateConnection();

        await connection.OpenAsync();

        // Check whether cart already exists
        const string cartQuery = @"
            SELECT
                Id,
                CustomerId,
                TotalProductCount,
                TotalPrice,
                CreatedAt,
                UpdatedAt
            FROM Carts
            WHERE CustomerId = @CustomerId";

        int cartId;

        await using (var cartCommand =
            new SqlCommand(cartQuery, connection))
        {
            cartCommand.Parameters.AddWithValue(
                "@CustomerId",
                customerId);

            await using var reader =
                await cartCommand.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                cartId = reader.GetInt32(
                    reader.GetOrdinal("Id"));
            }
            else
            {
                await reader.DisposeAsync();

                const string insertCartQuery = @"
                    INSERT INTO Carts
                    (
                        CustomerId,
                        TotalProductCount,
                        TotalPrice,
                        CreatedAt,
                        UpdatedAt
                    )
                    OUTPUT INSERTED.Id
                    VALUES
                    (
                        @CustomerId,
                        0,
                        0,
                        @CreatedAt,
                        @UpdatedAt
                    )";

                await using var insertCommand =
                    new SqlCommand(
                        insertCartQuery,
                        connection);

                insertCommand.Parameters.AddWithValue(
                    "@CustomerId",
                    customerId);

                insertCommand.Parameters.AddWithValue(
                    "@CreatedAt",
                    DateTime.UtcNow);

                insertCommand.Parameters.AddWithValue(
                    "@UpdatedAt",
                    DateTime.UtcNow);

                cartId = Convert.ToInt32(
                    await insertCommand.ExecuteScalarAsync());
            }
        }

        // Add / update products
        foreach (var product in products)
        {
            const string checkProductQuery = @"
                SELECT Quantity
                FROM CartProducts
                WHERE CartId = @CartId
                AND ProductId = @ProductId";

            int existingQuantity = 0;
            bool productExists = false;

            await using (var checkCommand =
                new SqlCommand(
                    checkProductQuery,
                    connection))
            {
                checkCommand.Parameters.AddWithValue(
                    "@CartId",
                    cartId);

                checkCommand.Parameters.AddWithValue(
                    "@ProductId",
                    product.ProductId);

                var result =
                    await checkCommand.ExecuteScalarAsync();

                if (result != null)
                {
                    productExists = true;
                    existingQuantity = Convert.ToInt32(result);
                }
            }

            if (productExists)
            {
                const string updateProductQuery = @"
                    UPDATE CartProducts
                    SET Quantity = Quantity + @Quantity
                    WHERE CartId = @CartId
                    AND ProductId = @ProductId";

                await using var updateCommand =
                    new SqlCommand(
                        updateProductQuery,
                        connection);

                updateCommand.Parameters.AddWithValue(
                    "@Quantity",
                    product.Quantity);

                updateCommand.Parameters.AddWithValue(
                    "@CartId",
                    cartId);

                updateCommand.Parameters.AddWithValue(
                    "@ProductId",
                    product.ProductId);

                await updateCommand.ExecuteNonQueryAsync();
            }
            else
            {
                const string insertProductQuery = @"
                    INSERT INTO CartProducts
                    (
                        CartId,
                        ProductId,
                        Quantity
                    )
                    VALUES
                    (
                        @CartId,
                        @ProductId,
                        @Quantity
                    )";

                await using var insertProductCommand =
                    new SqlCommand(
                        insertProductQuery,
                        connection);

                insertProductCommand.Parameters.AddWithValue(
                    "@CartId",
                    cartId);

                insertProductCommand.Parameters.AddWithValue(
                    "@ProductId",
                    product.ProductId);

                insertProductCommand.Parameters.AddWithValue(
                    "@Quantity",
                    product.Quantity);

                await insertProductCommand.ExecuteNonQueryAsync();
            }
        }

        await RecalculateCartTotalsAsync(
            connection,
            cartId);

        return await GetCartByIdAsync(
            connection,
            cartId);
    }

    public async Task<Cart?> RemoveFromCartAsync(
        int customerId,
        int productId)
    {
await using var connection = CreateConnection();

        await connection.OpenAsync();

        const string getCartQuery = @"
            SELECT Id
            FROM Carts
            WHERE CustomerId = @CustomerId";

        int cartId;

        await using (var command =
            new SqlCommand(
                getCartQuery,
                connection))
        {
            command.Parameters.AddWithValue(
                "@CustomerId",
                customerId);

            var result =
                await command.ExecuteScalarAsync();

            if (result == null)
                return null;

            cartId = Convert.ToInt32(result);
        }

        const string deleteQuery = @"
            DELETE FROM CartProducts
            WHERE CartId = @CartId
            AND ProductId = @ProductId";

        await using (var deleteCommand =
            new SqlCommand(
                deleteQuery,
                connection))
        {
            deleteCommand.Parameters.AddWithValue(
                "@CartId",
                cartId);

            deleteCommand.Parameters.AddWithValue(
                "@ProductId",
                productId);

            await deleteCommand.ExecuteNonQueryAsync();
        }

        await RecalculateCartTotalsAsync(
            connection,
            cartId);

        return await GetCartByIdAsync(
            connection,
            cartId);
    }

    public async Task<bool> ClearCartAsync(int customerId)
    {
    await using var connection = CreateConnection();
        await connection.OpenAsync();

        int cartId;

        const string getCartQuery = @"
            SELECT Id
            FROM Carts
            WHERE CustomerId = @CustomerId";

        await using (var command =
            new SqlCommand(
                getCartQuery,
                connection))
        {
            command.Parameters.AddWithValue(
                "@CustomerId",
                customerId);

            var result =
                await command.ExecuteScalarAsync();

            if (result == null)
                return false;

            cartId = Convert.ToInt32(result);
        }

        const string deleteProductsQuery = @"
            DELETE FROM CartProducts
            WHERE CartId = @CartId";

        await using (var deleteCommand =
            new SqlCommand(
                deleteProductsQuery,
                connection))
        {
            deleteCommand.Parameters.AddWithValue(
                "@CartId",
                cartId);

            await deleteCommand.ExecuteNonQueryAsync();
        }

        const string updateCartQuery = @"
            UPDATE Carts
            SET TotalProductCount = 0,
                TotalPrice = 0,
                UpdatedAt = @UpdatedAt
            WHERE Id = @CartId";

        await using var updateCommand =
            new SqlCommand(
                updateCartQuery,
                connection);

        updateCommand.Parameters.AddWithValue(
            "@UpdatedAt",
            DateTime.UtcNow);

        updateCommand.Parameters.AddWithValue(
            "@CartId",
            cartId);

        await updateCommand.ExecuteNonQueryAsync();

        return true;
    }

    private async Task RecalculateCartTotalsAsync(
        SqlConnection connection,
        int cartId)
    {
        const string query = @"
            SELECT
                COALESCE(SUM(cp.Quantity), 0) AS TotalProductCount,
                COALESCE(
                    SUM(cp.Quantity * p.Price),
                    0
                ) AS TotalPrice
            FROM CartProducts cp
            INNER JOIN Products p
                ON cp.ProductId = p.Id
            WHERE cp.CartId = @CartId";

        int totalProductCount = 0;
        decimal totalPrice = 0;

        await using (var command =
            new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue(
                "@CartId",
                cartId);

            await using var reader =
                await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                totalProductCount =
                    reader.GetInt32(
                        reader.GetOrdinal(
                            "TotalProductCount"));

                totalPrice =
                    reader.GetDecimal(
                        reader.GetOrdinal(
                            "TotalPrice"));
            }
        }

        const string updateQuery = @"
            UPDATE Carts
            SET TotalProductCount = @TotalProductCount,
                TotalPrice = @TotalPrice,
                UpdatedAt = @UpdatedAt
            WHERE Id = @CartId";

        await using var updateCommand =
            new SqlCommand(
                updateQuery,
                connection);

        updateCommand.Parameters.AddWithValue(
            "@TotalProductCount",
            totalProductCount);

        updateCommand.Parameters.AddWithValue(
            "@TotalPrice",
            totalPrice);

        updateCommand.Parameters.AddWithValue(
            "@UpdatedAt",
            DateTime.UtcNow);

        updateCommand.Parameters.AddWithValue(
            "@CartId",
            cartId);

        await updateCommand.ExecuteNonQueryAsync();
    }

    private async Task<Cart> GetCartByIdAsync(
        SqlConnection connection,
        int cartId)
    {
        const string query = @"
            SELECT
                Id,
                CustomerId,
                TotalProductCount,
                TotalPrice,
                CreatedAt,
                UpdatedAt
            FROM Carts
            WHERE Id = @CartId";

        await using var command =
            new SqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@CartId",
            cartId);

        await using var reader =
            await command.ExecuteReaderAsync();

        await reader.ReadAsync();

        return MapCart(reader);
    }

public async Task<bool> DeleteByCustomerIdAsync(int customerId)
    {
        const string query = @"
            DELETE FROM Carts
            WHERE CustomerId = @CustomerId";

        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@CustomerId",
            customerId);

        var rowsAffected =
            await command.ExecuteNonQueryAsync();

        return rowsAffected > 0;
    }


    public async Task<bool> DeleteAllAsync()
    {
        const string query = @"
            DELETE FROM Carts";

        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        var rowsAffected =
            await command.ExecuteNonQueryAsync();

        return rowsAffected > 0;
    }
    private static Cart MapCart(SqlDataReader reader)
    {
        return new Cart
        {
            Id = reader.GetInt32(
                reader.GetOrdinal("Id")),

            CustomerId = reader.GetInt32(
                reader.GetOrdinal("CustomerId")),

            TotalProductCount = reader.GetInt32(
                reader.GetOrdinal("TotalProductCount")),

            TotalPrice = reader.GetDecimal(
                reader.GetOrdinal("TotalPrice")),

            CreatedAt = reader.GetDateTime(
                reader.GetOrdinal("CreatedAt")),

            UpdatedAt = reader.GetDateTime(
                reader.GetOrdinal("UpdatedAt"))
        };
    }
}