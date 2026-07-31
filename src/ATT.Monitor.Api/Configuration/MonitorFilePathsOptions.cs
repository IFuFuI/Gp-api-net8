namespace ATT.Monitor.Api.Configuration;

/// <summary>Rutas de almacenamiento (paridad env <c>ARCHIVOS_ATM_PATH</c>, <c>JOURNALDIA</c> en API v1 <c>Connect</c>).</summary>
public sealed class MonitorFilePathsOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Raíz donde se guardan ZIP ATM (env <c>ARCHIVOS_ATM_PATH</c> si vacío en config).</summary>
    public string ArchivosAtmPath { get; set; } = string.Empty;

    /// <summary>Carpeta journal diario (env <c>JOURNALDIA</c>).</summary>
    public string JournalDiaPath { get; set; } = string.Empty;

    public static void ApplyEnvironmentDefaults(MonitorFilePathsOptions o)
    {
        if (string.IsNullOrWhiteSpace(o.ArchivosAtmPath))
            o.ArchivosAtmPath = Environment.GetEnvironmentVariable("ARCHIVOS_ATM_PATH") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(o.JournalDiaPath))
            o.JournalDiaPath = Environment.GetEnvironmentVariable("JOURNALDIA") ?? string.Empty;
    }
}
