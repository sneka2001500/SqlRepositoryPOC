
using Microsoft.AspNetCore.Mvc;
using SqlRepositoryPOC.Security;
using System.Security.Cryptography;

namespace SqlRepositoryPOC.Controllers;

public record SignatureRequest(
    string Message,
    string PrivateKey,
    string PublicKey);

[ApiController]
[Route("api/[controller]")]
public class SecurityController : ControllerBase
{
    [HttpPost("passwordHash")]
    public IActionResult PasswordHash([FromBody] PasswordRequest request)
    {
        var hasher = new PasswordService();
        var hash = hasher.HashPassword(request.UserId, request.Password);

        return Ok(new { hash });
    }

    [HttpPost("passwordVerify")]
    public IActionResult PasswordVerify([FromBody] PasswordVerifyRequest request)
    {
        var hasher = new PasswordService();
        var valid = hasher.VerifyPassword(
            request.UserId,
            request.Hash,
            request.Password);

        return Ok(new { valid });
    }

    [HttpPost("aesTest")]
    public IActionResult AesTest([FromBody] AesRequest request)
    {
        // Demo only: use a securely stored key in the real application.
        var key = RandomNumberGenerator.GetBytes(32);

        var encrypted = AesEncryption.Encrypt(request.Text, key);
        var decrypted = AesEncryption.Decrypt(encrypted, key);

        return Ok(new { encrypted, decrypted });
    }

    
[HttpGet("generateRsaKeys")]
public IActionResult GenerateRsaKeys()
{
    using var rsa = RSA.Create(2048);

    return Ok(new
    {
        privateKey = rsa.ExportPkcs8PrivateKeyPem(),
        publicKey = rsa.ExportSubjectPublicKeyInfoPem()
    });
}

[HttpPost("signatureTest")]
public IActionResult SignatureTest(
    [FromBody] SignatureRequest request)
{
    var signature = ApiSignatureService.Sign(
        request.Message,
        request.PrivateKey);

    var isValid = ApiSignatureService.Verify(
        request.Message,
        signature,
        request.PublicKey);

    var tamperedIsValid = ApiSignatureService.Verify(
        request.Message + "-modified",
        signature,
        request.PublicKey);

    return Ok(new
    {
        signature,
        validSignature = isValid,
        tamperedMessageValid = tamperedIsValid
    });
}

}

public record PasswordRequest(string UserId, string Password);
public record PasswordVerifyRequest(string UserId, string Hash, string Password);
public record AesRequest(string Text);
