namespace ATT.Monitor.Api.Abstractions;

/// <summary>Cifrado AES compatible con API v1 (<c>BC_MonitorAPIs.Services.Crypto</c>).</summary>
public interface ICrypto
{
    string Encrypt(string plainText);
    string Decrypt(string encryptedText);
}
