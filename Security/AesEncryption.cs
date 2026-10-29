
using System.Security.Cryptography;
using System.Text;

namespace SqlRepositoryPOC.Security;

public static class AesEncryption
{
    public static string Encrypt(string plainText, byte[] key)
    {
        using var aes = new AesGcm(key, 16);

        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] plaintext = Encoding.UTF8.GetBytes(plainText);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];

        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return Convert.ToBase64String(
            nonce.Concat(tag).Concat(ciphertext).ToArray());
    }

    public static string Decrypt(string encryptedText, byte[] key)
    {
        byte[] data = Convert.FromBase64String(encryptedText);

        if (data.Length < 28)
            throw new CryptographicException("Invalid encrypted data.");

        byte[] nonce = data[..12];
        byte[] tag = data[12..28];
        byte[] ciphertext = data[28..];
        byte[] plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
