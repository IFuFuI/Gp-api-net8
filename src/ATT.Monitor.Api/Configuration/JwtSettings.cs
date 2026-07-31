namespace ATT.Monitor.Api.Configuration;

/// <summary>
/// Credenciales de firma y validación JWT (paridad con API v1: variables JWT_SECRET, JWT_ISSUER, JWT_AUDIENCE).
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>Clave simétrica (mínimo 32 bytes recomendado para HS256).</summary>
    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Rellena desde <c>JWT_SECRET</c>, <c>JWT_ISSUER</c>, <c>JWT_AUDIENCE</c> si faltan en configuración.</summary>
    public static void ApplyEnvironmentDefaults(JwtSettings jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt.Secret))
            jwt.Secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(jwt.Issuer))
            jwt.Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(jwt.Audience))
            jwt.Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? string.Empty;
    }
}
