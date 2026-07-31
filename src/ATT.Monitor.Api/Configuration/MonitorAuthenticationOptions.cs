namespace ATT.Monitor.Api.Configuration;

/// <summary>
/// Activa el esquema Bearer JWT y la validación en base de datos tras <c>OnTokenValidated</c>.
/// </summary>
public sealed class MonitorAuthenticationOptions
{
    public const string SectionName = "Authentication";

    /// <summary>Si es <c>false</c>, la API arranca sin JWT (útil en desarrollo sin secretos).</summary>
    public bool EnableJwtBearer { get; set; } = true;
}
