
using System.Security.Cryptography;
using System.Text;

namespace SqlRepositoryPOC.Security;

public static class ApiSignatureService
{
    // System A signs the message using its private key.
    public static string Sign(string message, string privateKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);

        byte[] data = Encoding.UTF8.GetBytes(message);

        byte[] signature = rsa.SignData(
            data,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return Convert.ToBase64String(signature);
    }

    // System B verifies the signature using System A's public key.
    public static bool Verify(
        string message,
        string signatureBase64,
        string publicKeyPem)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            byte[] data = Encoding.UTF8.GetBytes(message);
            byte[] signature = Convert.FromBase64String(signatureBase64);

            return rsa.VerifyData(
                data,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
