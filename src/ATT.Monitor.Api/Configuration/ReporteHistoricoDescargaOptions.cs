namespace ATT.Monitor.Api.Configuration;

/// <summary>Rutas permitidas para servir archivos de <c>BD_REPORTES_HISTORICOS</c> (paridad API v1).</summary>
public sealed class ReporteHistoricoDescargaOptions
{
    public const string SectionName = "ReporteHistoricoDescarga";

    /// <summary>Rutas absolutas normalizadas (p. ej. <c>D:\Reportes</c>).</summary>
    public string[] AllowedPathPrefixes { get; set; } = [];

    /// <summary>
    /// Si no hay entradas en <see cref="AllowedPathPrefixes"/>, usa <see cref="MonitorFilePathsOptions.ArchivosAtmPath"/> como único prefijo permitido.
    /// </summary>
    public bool FallbackToPathArchivosAtmWhenPrefixesEmpty { get; set; } = true;
}
