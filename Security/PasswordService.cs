
using Microsoft.AspNetCore.Identity;

namespace SqlRepositoryPOC.Security;

public class PasswordService
{
    private readonly PasswordHasher<string> _hasher = new();

    public string HashPassword(string userId, string password)
    {
        return _hasher.HashPassword(userId, password);
    }

    public bool VerifyPassword(
        string userId,
        string hashedPassword,
        string suppliedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) ||
            string.IsNullOrEmpty(suppliedPassword))
        {
            return false;
        }

        try
        {
            var result = _hasher.VerifyHashedPassword(
                userId,
                hashedPassword,
                suppliedPassword
            );

            return result != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // The stored value is not a valid ASP.NET password hash.
            return false;
        }
    }
}
