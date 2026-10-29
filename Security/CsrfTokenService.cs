
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace SqlRepositoryPOC.Security;

public sealed class CsrfTokenService
{
    private readonly byte[] _key;

    public CsrfTokenService(IConfiguration configuration)
    {
        var secret = configuration["Csrf:Key"];

        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        {
            throw new InvalidOperationException(
                "Configure Csrf:Key with a secret of at least 32 characters.");
        }

        _key = Encoding.UTF8.GetBytes(secret);
    }

    public string GenerateToken(string userId)
    {
        var payload = $"{userId}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var signature = HMACSHA256.HashData(_key, payloadBytes);

        return $"{WebEncoders.Base64UrlEncode(payloadBytes)}.{WebEncoders.Base64UrlEncode(signature)}";
    }

    public bool IsValid(string? token, string userId)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2)
                return false;

            var payloadBytes = WebEncoders.Base64UrlDecode(parts[0]);
            var suppliedSignature = WebEncoders.Base64UrlDecode(parts[1]);
            var expectedSignature = HMACSHA256.HashData(_key, payloadBytes);

            if (!CryptographicOperations.FixedTimeEquals(
                suppliedSignature, expectedSignature))
                return false;

            var payload = Encoding.UTF8.GetString(payloadBytes);
            var fields = payload.Split(':');

            if (fields.Length != 3 || fields[0] != userId)
                return false;

            if (!long.TryParse(fields[1], out var issuedAt))
                return false;

            var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - issuedAt;

            return age >= 0 && age <= 3600;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
