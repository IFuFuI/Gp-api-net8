namespace ATT.Monitor.Api.Configuration;

/// <summary>AES-256-CBC con clave e IV en Base64 (paridad env <c>CrypAES</c> / <c>Cryp2AES</c> en API v1).</summary>
public sealed class MonitorCryptoOptions
{
    public const string SectionName = "Cryptography";

    /// <summary>Clave AES en Base64 (32 bytes → 44 caracteres típicos). Env: <c>CrypAES</c>.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>IV AES en Base64 (16 bytes). En v1 se llamaba <c>PublicKey</c> en <c>CryptoConfi</c>. Env: <c>Cryp2AES</c>.</summary>
    public string PublicKey { get; set; } = string.Empty;

    public static void ApplyEnvironmentDefaults(MonitorCryptoOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.SecretKey))
            o.SecretKey = Environment.GetEnvironmentVariable("CrypAES") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(o.PublicKey))
            o.PublicKey = Environment.GetEnvironmentVariable("Cryp2AES") ?? string.Empty;
    }
}
