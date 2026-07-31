namespace ATT.Monitor.Api.Configuration;

/// <summary>Opciones para listado y descarga de journal diario en disco (<see cref="MonitorFilePathsOptions.JournalDiaPath"/>).</summary>
public sealed class JournalHistoricoOptions
{
    public const string SectionName = "JournalHistorico";

    /// <summary>Rango máximo permitido entre desde y hasta (días).</summary>
    public int MaxRangeDays { get; set; } = 93;

    /// <summary>Máximo de filas devueltas por una consulta de listado (antes de paginar en memoria).</summary>
    public int MaxListScanFiles { get; set; } = 5000;

    /// <summary>Máximo de archivos seleccionables en una descarga.</summary>
    public int MaxDownloadSelection { get; set; } = 40;

    /// <summary>Tamaño máximo por parte al unificar texto (bytes).</summary>
    public int UnifiedPartMaxBytes { get; set; } = 8 * 1024 * 1024;

    /// <summary>Tamaño máximo leído por archivo de origen al unificar (bytes).</summary>
    public int MaxSourceFileReadBytes { get; set; } = 16 * 1024 * 1024;

    /// <summary>Vida útil máxima de un job finalizado antes de borrar entrada y ZIP temporal (minutos).</summary>
    public int ExportJobRetentionMinutes { get; set; } = 180;
}
