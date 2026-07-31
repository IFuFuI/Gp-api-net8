using ATT.Monitor.Api.Models.Dashboard;

namespace ATT.Monitor.Api.Abstractions;

/// <summary>Paridad <c>IReporteHistoricoDescargaService</c> API v1.</summary>
public interface IReporteHistoricoDescargaService
{
    Task<ReporteHistoricoFileOpenResult?> TryOpenHistoricoAsync(int idReportesHistorico, CancellationToken cancellationToken = default);
}
