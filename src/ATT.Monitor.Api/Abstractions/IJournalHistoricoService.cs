using ATT.Monitor.Api.Models.JournalHistorico;

namespace ATT.Monitor.Api.Abstractions;

public interface IJournalHistoricoService
{
    Task<JournalHistoricoListResponse> ListarAsync(JournalHistoricoListRequest request, CancellationToken cancellationToken = default);

    Task<(Stream Stream, string FileName, JournalHistoricoDownloadSummaryDto Summary, bool DeletePath)> CrearDescargaZipAsync(
        JournalHistoricoDownloadRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Escribe ZIP de exportación y devuelve metadatos (job en segundo plano).</summary>
    Task<(string ZipFileName, JournalHistoricoDownloadSummaryDto Summary)> WriteExportZipToFileAsync(
        JournalHistoricoDownloadRequest request,
        string outputPath,
        CancellationToken cancellationToken = default);
}
