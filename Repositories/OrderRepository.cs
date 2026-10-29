using Microsoft.Data.SqlClient;
using SqlRepositoryPOC.Models;

namespace SqlRepositoryPOC.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly string _connectionString;

    public OrderRepository(IConfiguration configuration)
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

    public async Task<Order> CreateAsync(
        Order order,
        List<OrderProduct> products)
    {
     await using var connection = CreateConnection();

        await connection.OpenAsync();

        const string orderQuery = @"
            INSERT INTO Orders
            (
                CustomerId,
                TotalPrice,
                Status,
                DeliveryDate,
                ShippingAddress,
                PaymentMode,
                PaymentId,
                CreatedAt,
                UpdatedAt
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @CustomerId,
                @TotalPrice,
                @Status,
                @DeliveryDate,
                @ShippingAddress,
                @PaymentMode,
                @PaymentId,
                @CreatedAt,
                @UpdatedAt
            )";

        await using var orderCommand =
            new SqlCommand(orderQuery, connection);

        orderCommand.Parameters.AddWithValue(
            "@CustomerId",
            order.CustomerId);

        orderCommand.Parameters.AddWithValue(
            "@TotalPrice",
            order.TotalPrice);

        orderCommand.Parameters.AddWithValue(
            "@Status",
            order.Status);

        orderCommand.Parameters.AddWithValue(
            "@DeliveryDate",
            order.DeliveryDate);

        orderCommand.Parameters.AddWithValue(
            "@ShippingAddress",
            order.ShippingAddress ?? (object)DBNull.Value);

        orderCommand.Parameters.AddWithValue(
            "@PaymentMode",
            order.PaymentMode);

        orderCommand.Parameters.AddWithValue(
            "@PaymentId",
            order.PaymentId ?? (object)DBNull.Value);

        orderCommand.Parameters.AddWithValue(
            "@CreatedAt",
            order.CreatedAt);

        orderCommand.Parameters.AddWithValue(
            "@UpdatedAt",
            order.UpdatedAt);

        var result =
            await orderCommand.ExecuteScalarAsync();

        order.Id = Convert.ToInt32(result);

        // Insert order products
        foreach (var product in products)
        {
            const string productQuery = @"
                INSERT INTO OrderProducts
                (
                    OrderId,
                    ProductId,
                    Quantity
                )
                VALUES
                (
                    @OrderId,
                    @ProductId,
                    @Quantity
                )";

            await using var productCommand =
                new SqlCommand(
                    productQuery,
                    connection);

            productCommand.Parameters.AddWithValue(
                "@OrderId",
                order.Id);

            productCommand.Parameters.AddWithValue(
                "@ProductId",
                product.ProductId);

            productCommand.Parameters.AddWithValue(
                "@Quantity",
                product.Quantity);

            await productCommand.ExecuteNonQueryAsync();
        }

        return order;
    }

    public async Task<List<Order>> GetAllAsync()
    {
        const string query = @"
            SELECT
                Id,
                CustomerId,
                TotalPrice,
                Status,
                DeliveryDate,
                ShippingAddress,
                PaymentMode,
                PaymentId,
                CreatedAt,
                UpdatedAt
            FROM Orders";

        var orders = new List<Order>();

        await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            orders.Add(MapOrder(reader));
        }

        return orders;
    }

    public async Task<List<Order>> GetByEmailAsync(
        string email)
    {
        const string query = @"
            SELECT
                o.Id,
                o.CustomerId,
                o.TotalPrice,
                o.Status,
                o.DeliveryDate,
                o.ShippingAddress,
                o.PaymentMode,
                o.PaymentId,
                o.CreatedAt,
                o.UpdatedAt
            FROM Orders o
            INNER JOIN Customers c
                ON o.CustomerId = c.Id
            WHERE c.Email = @Email";

        var orders = new List<Order>();

    await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        command.Parameters.AddWithValue(
            "@Email",
            email);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            orders.Add(MapOrder(reader));
        }

        return orders;
    }

    public async Task<Order?> UpdateAsync(
        int orderId,
        string status,
        DateTime deliveryDate)
    {
        const string updateQuery = @"
            UPDATE Orders
            SET
                Status = @Status,
                DeliveryDate = @DeliveryDate,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

      await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                updateQuery,
                connection);

        command.Parameters.AddWithValue(
            "@Id",
            orderId);

        command.Parameters.AddWithValue(
            "@Status",
            status);

        command.Parameters.AddWithValue(
            "@DeliveryDate",
            deliveryDate);

        command.Parameters.AddWithValue(
            "@UpdatedAt",
            DateTime.UtcNow);

        var rowsAffected =
            await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
            return null;

        const string selectQuery = @"
            SELECT
                Id,
                CustomerId,
                TotalPrice,
                Status,
                DeliveryDate,
                ShippingAddress,
                PaymentMode,
                PaymentId,
                CreatedAt,
                UpdatedAt
            FROM Orders
            WHERE Id = @Id";

        await using var selectCommand =
            new SqlCommand(
                selectQuery,
                connection);

        selectCommand.Parameters.AddWithValue(
            "@Id",
            orderId);

        await using var reader =
            await selectCommand.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return MapOrder(reader);
    }

    private static Order MapOrder(
        SqlDataReader reader)
    {
        return new Order
        {
            Id = reader.GetInt32(
                reader.GetOrdinal("Id")),

            CustomerId = reader.GetInt32(
                reader.GetOrdinal("CustomerId")),

            TotalPrice = reader.GetDecimal(
                reader.GetOrdinal("TotalPrice")),

            Status = reader.GetString(
                reader.GetOrdinal("Status")),

            DeliveryDate = reader.GetDateTime(
                reader.GetOrdinal("DeliveryDate")),

            ShippingAddress = reader.IsDBNull(
                reader.GetOrdinal("ShippingAddress"))
                ? null
                : reader.GetString(
                    reader.GetOrdinal("ShippingAddress")),

            PaymentMode = reader.GetString(
                reader.GetOrdinal("PaymentMode")),

            PaymentId = reader.IsDBNull(
                reader.GetOrdinal("PaymentId"))
                ? null
                : reader.GetString(
                    reader.GetOrdinal("PaymentId")),

            CreatedAt = reader.GetDateTime(
                reader.GetOrdinal("CreatedAt")),

            UpdatedAt = reader.GetDateTime(
                reader.GetOrdinal("UpdatedAt"))
        };
    }
}