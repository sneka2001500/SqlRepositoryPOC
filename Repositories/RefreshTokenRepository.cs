using Microsoft.Data.SqlClient;
using SqlRepositoryPOC.Models;

namespace SqlRepositoryPOC.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly string _connectionString;

    public RefreshTokenRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");
    }

    private SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
       const string query = @"
            INSERT INTO RefreshTokens
            (
                CustomerId,
                Token,
                ExpiresAt,
                CreatedAt,
                RevokedAt
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @CustomerId,
                @Token,
                @ExpiresAt,
                @CreatedAt,
                @RevokedAt
            );";

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@CustomerId", refreshToken.CustomerId);
        command.Parameters.AddWithValue("@Token", refreshToken.Token);
        command.Parameters.AddWithValue("@ExpiresAt", refreshToken.ExpiresAt);
        command.Parameters.AddWithValue("@CreatedAt", refreshToken.CreatedAt);
        command.Parameters.AddWithValue(
            "@RevokedAt",
            (object?)refreshToken.RevokedAt ?? DBNull.Value
        );

        refreshToken.Id = Convert.ToInt32(
            await command.ExecuteScalarAsync()
        );

        return refreshToken;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        const string query = @"
            SELECT
                Id,
                CustomerId,
                Token,
                ExpiresAt,
                CreatedAt,
                RevokedAt
            FROM RefreshTokens
            WHERE Token = @Token;";

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@Token", token);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new RefreshToken
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
            Token = reader.GetString(reader.GetOrdinal("Token")),
            ExpiresAt = reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            RevokedAt = reader.IsDBNull(reader.GetOrdinal("RevokedAt"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("RevokedAt"))
        };
    }

    public async Task<bool> RevokeAsync(string token)
    {
        const string query = @"
            UPDATE RefreshTokens
            SET RevokedAt = @RevokedAt
            WHERE Token = @Token
              AND RevokedAt IS NULL;";

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@Token", token);
        command.Parameters.AddWithValue("@RevokedAt", DateTime.UtcNow);

        var rowsAffected = await command.ExecuteNonQueryAsync();

        return rowsAffected > 0;
    }
}