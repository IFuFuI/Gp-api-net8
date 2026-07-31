using ATT.Monitor.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ATT.Monitor.Api.Infrastructure.Persistence;

/// <summary>Resuelve carpetas permitidas para escribir/leer reportes históricos.</summary>
public static class ReporteHistoricoPathHelper
{
    public const string MonitorReportesSubfolder = "ReportesMonitor";

    public static string[] ResolveEffectivePrefixes(
        ReporteHistoricoDescargaOptions opt,
        MonitorFilePathsOptions filePaths,
        ILogger? logger = null)
    {
        if (opt.AllowedPathPrefixes is { Length: > 0 })
            return opt.AllowedPathPrefixes;

        if (!opt.FallbackToPathArchivosAtmWhenPrefixesEmpty)
            return [];

        var raw = filePaths.ArchivosAtmPath;
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        try
        {
            return [Path.GetFullPath(raw.Trim())];
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "No se pudo normalizar FileStorage:ArchivosAtmPath.");
            return [];
        }
    }

    public static string? TryResolveOutputDirectory(
        ReporteHistoricoDescargaOptions opt,
        MonitorFilePathsOptions filePaths,
        ILogger? logger = null)
    {
        var prefixes = ResolveEffectivePrefixes(opt, filePaths, logger);
        if (prefixes.Length == 0)
            return null;

        var root = prefixes[0].TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var dir = Path.Combine(root, MonitorReportesSubfolder);
        try
        {
            Directory.CreateDirectory(dir);
            return Path.GetFullPath(dir);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "No se pudo crear carpeta de salida de reportes en {Dir}.", dir);
            return null;
        }
    }
}
