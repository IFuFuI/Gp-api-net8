using ATT.Monitor.Api.Models.JournalHistorico;

namespace ATT.Monitor.Api.Abstractions;

public interface IJournalHistoricoExportJobStore
{
    /// <summary>Un job activo = Pending | Processing. Un usuario sólo puede tener uno.</summary>
    bool TryCreateJob(string userId, JournalHistoricoDownloadRequest payload, out string jobId, out string? rejectionReason);

    JournalHistoricoExportJobStatusDto? GetJob(string requestingUserId, string jobId);

    /// <summary>Job pendiente/en ejecución del usuario si existe.</summary>
    JournalHistoricoExportJobStatusDto? GetActiveJobForUser(string userId);

    /// <returns>Null si no listo / no autorizado.</returns>
    Task<(Stream? Content, string FileName)> OpenDownloadAsync(string requestingUserId, string jobId, CancellationToken ct);

    void CleanupExpiredJobs();
}
