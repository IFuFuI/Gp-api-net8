namespace ATT.Monitor.Api.Models.Dashboard;

/// <summary>Archivo listo para respuesta HTTP de descarga (paridad API v1).</summary>
public sealed class ReporteHistoricoFileOpenResult
{
    public ReporteHistoricoFileOpenResult(Stream stream, string downloadFileName, string contentType)
    {
        Stream = stream;
        DownloadFileName = downloadFileName;
        ContentType = contentType;
    }

    public Stream Stream { get; }
    public string DownloadFileName { get; }
    public string ContentType { get; }
}
