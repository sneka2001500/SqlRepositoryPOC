using Microsoft.Data.SqlClient;
using SqlRepositoryPOC.Models;
using Microsoft.Extensions.Caching.Memory;
namespace SqlRepositoryPOC.Repositories;


public class CustomerRepository : ICustomerRepository
{
   private readonly string _connectionString;
private readonly IMemoryCache _cache;

public CustomerRepository(
    IConfiguration configuration,
    IMemoryCache cache)
{
    _connectionString =
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' not found.");

    _cache = cache;
}

    private SqlConnection CreateConnection()
{
    return new SqlConnection(_connectionString);
}

// public async Task<bool> TransactionTestAsync(int customerId)
// {
//     await using var connection = CreateConnection();
//     await connection.OpenAsync();

//   await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

//     try
//     {
//         const string query = @"
//             UPDATE Customers
//             SET Name = @Name
//             WHERE Id = @Id";

//         await using var command = new SqlCommand(query, connection, transaction);

//         command.Parameters.AddWithValue("@Name", "DotNet Transaction Test");
//         command.Parameters.AddWithValue("@Id", customerId);

//         await command.ExecuteNonQueryAsync();

//         // COMMIT
//         await transaction.CommitAsync();

//         return true;
//     }
//     catch
//     {
//         // ROLLBACK
//         await transaction.RollbackAsync();

//         return false;
//     }
// }



public async Task<bool> TransactionTestAsync(int customerId)
{
    await using var connection = CreateConnection();
    await connection.OpenAsync();

    await using var transaction =
        (SqlTransaction)await connection.BeginTransactionAsync();

    try
    {
        const string query = @"
            UPDATE Customers
            SET Name = @Name
            WHERE Id = @Id";

        await using var command =
            new SqlCommand(query, connection, transaction);

        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 200)
            .Value = "Transaction Test";

        command.Parameters.Add("@Id", System.Data.SqlDbType.Int)
            .Value = customerId;

        int rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            await transaction.RollbackAsync();
            return false;
        }

        await transaction.CommitAsync();
        return true;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

    
    // GET ALL CUSTOMERS

public async Task<List<Customer>> GetAllAsync()
{
    const string cacheKey = "customers_all";

    if (_cache.TryGetValue(
            cacheKey,
            out List<Customer>? cachedCustomers)
        && cachedCustomers != null)
    {
        return cachedCustomers;
    }

    const string query = @"
        SELECT
            Id, Name, Age, Email, Photo, Address,
            IsGuest, Role, CreatedAt, UpdatedAt
        FROM Customers";

    var customers = new List<Customer>();

    await using var connection = CreateConnection();
    await connection.OpenAsync();

    await using var command = new SqlCommand(query, connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        // Map only non-sensitive fields.
        customers.Add(new Customer
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),

            Name = reader.IsDBNull(reader.GetOrdinal("Name"))
                ? null : reader.GetString(reader.GetOrdinal("Name")),

            Age = reader.IsDBNull(reader.GetOrdinal("Age"))
                ? null : reader.GetInt32(reader.GetOrdinal("Age")),

            Email = reader.IsDBNull(reader.GetOrdinal("Email"))
                ? null : reader.GetString(reader.GetOrdinal("Email")),

            // Never cache the password hash.
            Password = null,

            Photo = reader.IsDBNull(reader.GetOrdinal("Photo"))
                ? null : reader.GetString(reader.GetOrdinal("Photo")),

            Address = reader.IsDBNull(reader.GetOrdinal("Address"))
                ? null : reader.GetString(reader.GetOrdinal("Address")),

            IsGuest = reader.GetBoolean(reader.GetOrdinal("IsGuest")),

            Role = reader.GetString(reader.GetOrdinal("Role")),

            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),

            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        });
    }

    _cache.Set(
        cacheKey,
        customers,
        TimeSpan.FromMinutes(5));

    return customers;
}

public async Task<List<Customer>> GetPagedAsync(
    int pageNumber,
    int pageSize)
{
    const string query = @"
        SELECT
            Id,
            Name,
            Age,
            Email,
            Password,
            Photo,
            Address,
            IsGuest,
            Role,
            CreatedAt,
            UpdatedAt
        FROM Customers
        ORDER BY Id
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY";

    var customers = new List<Customer>();

    await using var connection = CreateConnection();
    await connection.OpenAsync();

    await using var command = new SqlCommand(query, connection);

    int offset = (pageNumber - 1) * pageSize;

    command.Parameters.Add("@Offset", System.Data.SqlDbType.Int)
        .Value = offset;

    command.Parameters.Add("@PageSize", System.Data.SqlDbType.Int)
        .Value = pageSize;

    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        customers.Add(MapCustomer(reader));
    }

    return customers;
}


public async Task<List<Customer>> GetFilteredAndSortedAsync(
    string? name,
    string? email,
    string? sortBy,
    string? sortDirection,
    int pageNumber,
    int pageSize)
{
    var allowedSortColumns = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "Id",
        ["name"] = "Name",
        ["age"] = "Age",
        ["email"] = "Email",
        ["createdAt"] = "CreatedAt"
    };

    sortBy = string.IsNullOrWhiteSpace(sortBy) ? "id" : sortBy;

    if (!allowedSortColumns.TryGetValue(sortBy, out var sortColumn))
    {
        throw new ArgumentException("Invalid sort field.");
    }

    var direction = string.Equals(
        sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
        ? "DESC"
        : "ASC";

    const string baseQuery = @"
        SELECT
            Id, Name, Age, Email, Password, Photo, Address,
            IsGuest, Role, CreatedAt, UpdatedAt
        FROM Customers
        WHERE (@Name IS NULL OR Name LIKE '%' + @Name + '%')
          AND (@Email IS NULL OR Email LIKE '%' + @Email + '%')
        ";

   
var orderBy = sortColumn.Equals(
    "Id",
    StringComparison.OrdinalIgnoreCase)
    ? $"Id {direction}"
    : $"{sortColumn} {direction}, Id ASC";

var query = $@"
    {baseQuery}
    ORDER BY {orderBy}
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY";

    var customers = new List<Customer>();

    await using var connection = CreateConnection();
    await connection.OpenAsync();

    await using var command = new SqlCommand(query, connection);

    command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 200)
        .Value = string.IsNullOrWhiteSpace(name) ? DBNull.Value : name;

    command.Parameters.Add("@Email", System.Data.SqlDbType.NVarChar, 320)
        .Value = string.IsNullOrWhiteSpace(email) ? DBNull.Value : email;

    command.Parameters.Add("@Offset", System.Data.SqlDbType.Int)
        .Value = (pageNumber - 1) * pageSize;

    command.Parameters.Add("@PageSize", System.Data.SqlDbType.Int)
        .Value = pageSize;

    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        customers.Add(MapCustomer(reader));
    }

    return customers;
}

    // GET CUSTOMER BY ID
    public async Task<Customer?> GetByIdAsync(int id)
    {
        const string query = @"
            SELECT
                Id,
                Name,
                Age,
                Email,
                Password,
                Photo,
                Address,
                IsGuest,
                Role,
                CreatedAt,
                UpdatedAt
            FROM Customers
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

        return MapCustomer(reader);
    }

    // GET CUSTOMER BY EMAIL
    public async Task<Customer?> GetByEmailAsync(string email)
    {
        const string query = @"
            SELECT
                Id,
                Name,
                Age,
                Email,
                Password,
                Photo,
                Address,
                IsGuest,
                Role,
                CreatedAt,
                UpdatedAt
            FROM Customers
            WHERE Email = @Email";

       await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@Email", email);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapCustomer(reader);
    }

    // CREATE CUSTOMER
    public async Task<Customer> CreateAsync(Customer customer)
    {
        const string query = @"
            INSERT INTO Customers
            (
                Name,
                Age,
                Email,
                Password,
                Photo,
                Address,
                IsGuest,
                CreatedAt,
                UpdatedAt
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @Name,
                @Age,
                @Email,
                @Password,
                @Photo,
                @Address,
                @IsGuest,
                @CreatedAt,
                @UpdatedAt
            )";

      await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        AddCustomerParameters(command, customer);

        var result =
            await command.ExecuteScalarAsync();

        customer.Id = Convert.ToInt32(result);
        InvalidateCustomerCache();

        return customer;
    }

    // UPDATE CUSTOMER
    public async Task<bool> UpdateAsync(Customer customer)
    {
        const string query = @"
            UPDATE Customers
            SET
                Name = @Name,
                Age = @Age,
                Email = @Email,
                Password = @Password,
                Photo = @Photo,
                Address = @Address,
                IsGuest = @IsGuest,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@Id", customer.Id);

        command.Parameters.AddWithValue(
            "@Name",
            customer.Name ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "@Age",
            customer.Age ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "@Email",
            customer.Email ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "@Password",
            customer.Password ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "@Photo",
            customer.Photo ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "@Address",
            customer.Address ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "@IsGuest",
            customer.IsGuest);

        command.Parameters.AddWithValue(
            "@UpdatedAt",
            customer.UpdatedAt);

        var rowsAffected =
            await command.ExecuteNonQueryAsync();
if (rowsAffected > 0)
{
    InvalidateCustomerCache();
    return true;
}

return false;
    }

    // DELETE CUSTOMER BY EMAIL
    public async Task<bool> DeleteByEmailAsync(string email)
    {
        const string query = @"
            DELETE FROM Customers
            WHERE Email = @Email";

    await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@Email", email);

        var rowsAffected =
            await command.ExecuteNonQueryAsync();

       if (rowsAffected > 0)
{
    InvalidateCustomerCache();
    return true;
}

return false;
    }

    // DELETE ALL CUSTOMERS
    public async Task<bool> DeleteAllAsync()
    {
        const string query = @"
            DELETE FROM Customers";

     await using var connection = CreateConnection();

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(query, connection);

        await command.ExecuteNonQueryAsync();
        InvalidateCustomerCache();

        return true;
    }

    // CONVERT SQL ROW → CUSTOMER OBJECT
    private static Customer MapCustomer(SqlDataReader reader)
    {
        return new Customer
        {
            Id = reader.GetInt32(
                reader.GetOrdinal("Id")),

            Name = reader.IsDBNull(
                reader.GetOrdinal("Name"))
                ? null
                : reader.GetString(
                    reader.GetOrdinal("Name")),

            Age = reader.IsDBNull(
                reader.GetOrdinal("Age"))
                ? null
                : reader.GetInt32(
                    reader.GetOrdinal("Age")),

            Email = reader.IsDBNull(
                reader.GetOrdinal("Email"))
                ? null
                : reader.GetString(
                    reader.GetOrdinal("Email")),

            Password = reader.IsDBNull(
                reader.GetOrdinal("Password"))
                ? null
                : reader.GetString(
                    reader.GetOrdinal("Password")),

            Photo = reader.IsDBNull(
                reader.GetOrdinal("Photo"))
                ? null
                : reader.GetString(
                    reader.GetOrdinal("Photo")),

            Address = reader.IsDBNull(
                reader.GetOrdinal("Address"))
                ? null
                : reader.GetString(
                    reader.GetOrdinal("Address")),

            IsGuest = reader.GetBoolean(
                reader.GetOrdinal("IsGuest")),

                Role = reader.GetString(reader.GetOrdinal("Role")),

            CreatedAt = reader.GetDateTime(
                reader.GetOrdinal("CreatedAt")),

            UpdatedAt = reader.GetDateTime(
                reader.GetOrdinal("UpdatedAt"))
        };
    }
private void InvalidateCustomerCache()
{
    _cache.Remove("customers_all");
}


    // ADD PARAMETERS FOR INSERT
    private static void AddCustomerParameters(
        SqlCommand command,
        Customer customer)
    {
        command.Parameters.AddWithValue(
            "@Name",
            customer.Name ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "@Age",
            customer.Age ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "@Email",
            customer.Email ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "@Password",
            customer.Password ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "@Photo",
            customer.Photo ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "@Address",
            customer.Address ?? (object)DBNull.Value);

        command.Parameters.AddWithValue(
            "@IsGuest",
            customer.IsGuest);

        command.Parameters.AddWithValue(
            "@CreatedAt",
            customer.CreatedAt);

        command.Parameters.AddWithValue(
            "@UpdatedAt",
            customer.UpdatedAt);
    }
}