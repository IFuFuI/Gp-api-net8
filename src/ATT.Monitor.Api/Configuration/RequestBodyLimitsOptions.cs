namespace ATT.Monitor.Api.Configuration;

/// <summary>
/// Límite del cuerpo HTTP para subidas multipart (ZIP). Paridad con API v1: variable de entorno <c>MAX_REQUEST_BODY_MB</c>
/// (si está en rango, sustituye <see cref="MaxMegabytes"/>), sección <c>RequestBodyLimits:MaxMegabytes</c>, y aplicación en
/// Kestrel, IIS (<see cref="Microsoft.AspNetCore.Server.IIS.IISServerOptions"/> <c>MaxRequestBodySize</c>),
/// <see cref="Microsoft.AspNetCore.Http.Features.FormOptions"/> <c>MultipartBodyLengthLimit</c> y metadatos públicos
/// <c>GET api/public/upload-limits</c>.
/// En IIS clásico, además configure <c>system.webServer/security/requestFiltering/requestLimits@maxAllowedContentLength</c>
/// (en bytes) ≥ al límite efectivo en MB, o el filtro IIS puede devolver 413 antes que el host ASP.NET Core.
/// </summary>
public sealed class RequestBodyLimitsOptions
{
    public const string SectionName = "RequestBodyLimits";

    /// <summary>Rango permitido 32–2048 (MB), como la API legado.</summary>
    public const int MinMegabytes = 32;

    public const int MaxMegabytesCap = 2048;

    /// <summary>Tamaño máximo por defecto (MB).</summary>
    public int MaxMegabytes { get; set; } = 200;

    /// <summary>
    /// Si la variable de entorno <c>MAX_REQUEST_BODY_MB</c> está definida y en rango, sustituye <see cref="MaxMegabytes"/>.
    /// </summary>
    public static void ApplyEnvironmentDefaults(RequestBodyLimitsOptions o)
    {
        var env = Environment.GetEnvironmentVariable("MAX_REQUEST_BODY_MB");
        if (string.IsNullOrWhiteSpace(env))
            return;
        if (!long.TryParse(env, out var mb))
            return;
        if (mb is < MinMegabytes or > MaxMegabytesCap)
            return;
        o.MaxMegabytes = (int)mb;
    }

    public long ResolveMaxBytes()
    {
        var mb = Math.Clamp(MaxMegabytes, MinMegabytes, MaxMegabytesCap);
        return mb * 1024L * 1024L;
    }
}
