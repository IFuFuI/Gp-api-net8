using System.Data;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Configuration;
using ATT.Monitor.Api.Models.Dashboard;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace ATT.Monitor.Api.Infrastructure.Persistence;

/// <summary>Paridad <c>ReporteHistoricoDescargaService</c> API v1.</summary>
public sealed class ReporteHistoricoDescargaService(
    IConfiguration configuration,
    IOptionsMonitor<ReporteHistoricoDescargaOptions> options,
    IOptionsMonitor<MonitorFilePathsOptions> filePathsOptions,
    ILogger<ReporteHistoricoDescargaService> logger) : IReporteHistoricoDescargaService
{
    private string ConnectionString =>
        configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:SqlServer.");

    public async Task<ReporteHistoricoFileOpenResult?> TryOpenHistoricoAsync(
        int idReportesHistorico,
        CancellationToken cancellationToken = default)
    {
        if (idReportesHistorico <= 0)
            return null;

        var prefixes = ResolveEffectivePrefixes(options.CurrentValue);
        if (prefixes.Length == 0)
        {
            logger.LogWarning(
                "Descarga reporte histórico deshabilitada: sin prefijos (configure {Section} o FileStorage:ArchivosAtmPath).",
                ReporteHistoricoDescargaOptions.SectionName);
            return null;
        }

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT RUTA, NOMBRE_ARCHIVO, EXTENSION
            FROM dbo.BD_REPORTES_HISTORICOS
            WHERE ID_REPORTES_HISTORICO = @Id
            """;

        var row = await connection.QueryFirstOrDefaultAsync<HistoricoPathRow>(
            new CommandDefinition(sql, new { Id = idReportesHistorico }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (row is null)
            return null;

        var dir = (row.RUTA ?? string.Empty).Trim();
        var rawName = (row.NOMBRE_ARCHIVO ?? string.Empty).Trim();
        var fileName = Path.GetFileName(rawName);
        if (string.IsNullOrEmpty(fileName) || !string.Equals(fileName, rawName, StringComparison.OrdinalIgnoreCase))
            return null;

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(Path.Combine(dir, fileName));
        }
        catch (Exception)
        {
            return null;
        }

        if (!IsUnderAllowedRoot(fullPath, prefixes))
            return null;

        if (!File.Exists(fullPath))
            return null;

        var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            65536,
            FileOptions.Asynchronous);

        var downloadName = SanitizeDownloadName(fileName);
        var contentType = ResolveContentType(row.EXTENSION);

        return new ReporteHistoricoFileOpenResult(stream, downloadName, contentType);
    }

    private string[] ResolveEffectivePrefixes(ReporteHistoricoDescargaOptions opt)
    {
        if (opt.AllowedPathPrefixes is { Length: > 0 })
            return opt.AllowedPathPrefixes;

        if (!opt.FallbackToPathArchivosAtmWhenPrefixesEmpty)
            return [];

        var raw = filePathsOptions.CurrentValue.ArchivosAtmPath;
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        try
        {
            return [Path.GetFullPath(raw.Trim())];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo normalizar FileStorage:ArchivosAtmPath para prefijo de descarga.");
            return [];
        }
    }

    private static bool IsUnderAllowedRoot(string candidateFullPath, string[] prefixes)
    {
        foreach (var p in prefixes)
        {
            if (string.IsNullOrWhiteSpace(p))
                continue;

            string root;
            try
            {
                root = Path.GetFullPath(p.Trim());
            }
            catch (Exception)
            {
                continue;
            }

            if (string.IsNullOrEmpty(root))
                continue;

            var sep = Path.DirectorySeparatorChar;
            if (!root.EndsWith(sep) && !root.EndsWith(Path.AltDirectorySeparatorChar))
                root += sep;

            if (candidateFullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(candidateFullPath, root.TrimEnd(sep, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string SanitizeDownloadName(string name)
    {
        var n = Path.GetFileName(name.Trim());
        if (string.IsNullOrEmpty(n))
            return "reporte.bin";
        foreach (var c in Path.GetInvalidFileNameChars())
            n = n.Replace(c, '_');
        return n;
    }

    private static string ResolveContentType(string? extension)
    {
        var e = (extension ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        return e switch
        {
            "csv" => "text/csv; charset=utf-8",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "xls" => "application/vnd.ms-excel",
            "pdf" => "application/pdf",
            "txt" => "text/plain; charset=utf-8",
            "zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }

    private sealed class HistoricoPathRow
    {
        public string? RUTA { get; set; }
        public string? NOMBRE_ARCHIVO { get; set; }
        public string? EXTENSION { get; set; }
    }
}
