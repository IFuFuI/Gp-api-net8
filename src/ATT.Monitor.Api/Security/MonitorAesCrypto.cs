using System.Security.Cryptography;
using System.Text;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ATT.Monitor.Api.Security;

/// <summary>Implementación <c>Crypto</c> v1: AES, clave e IV desde Base64.</summary>
public sealed class MonitorAesCrypto(IOptionsMonitor<MonitorCryptoOptions> options) : ICrypto
{
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            throw new ArgumentException("Texto vacío", nameof(plainText));

        var o = options.CurrentValue;
        ValidateConfigured(o);

        var keyBytes = Convert.FromBase64String(o.SecretKey);
        var ivBytes = Convert.FromBase64String(o.PublicKey);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = ivBytes;

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(plainBytes, 0, plainBytes.Length);
            cs.FlushFinalBlock();
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrWhiteSpace(encryptedText))
            throw new ArgumentException("Texto vacío", nameof(encryptedText));

        var o = options.CurrentValue;
        ValidateConfigured(o);

        var keyBytes = Convert.FromBase64String(o.SecretKey);
        var ivBytes = Convert.FromBase64String(o.PublicKey);
        var cipherBytes = Convert.FromBase64String(encryptedText);

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.IV = ivBytes;

        using var ms = new MemoryStream(cipherBytes);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var resultStream = new MemoryStream();
        cs.CopyTo(resultStream);
        return Encoding.UTF8.GetString(resultStream.ToArray());
    }

    private static void ValidateConfigured(MonitorCryptoOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.SecretKey) || string.IsNullOrWhiteSpace(o.PublicKey))
        {
            throw new InvalidOperationException(
                "Cifrado no configurado: Cryptography:SecretKey / Cryptography:PublicKey (IV) o variables CrypAES / Cryp2AES.");
        }
    }
}
