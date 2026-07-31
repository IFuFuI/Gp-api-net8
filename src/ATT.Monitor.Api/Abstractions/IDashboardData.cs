using ATT.Monitor.Api.Models.Dashboard;
using ATT.Monitor.Api.Models.Login;

namespace ATT.Monitor.Api.Abstractions;

/// <summary>Paridad <c>IDashboardData</c> / <c>DashboardData.cs</c> API v1.</summary>
public interface IDashboardData
{
    Task<IEnumerable<FallaResponse>> GetFallasAsync(FallaRequest request, CancellationToken cancellationToken = default);
    Task<MCards?> GetCardsAsync(CancellationToken cancellationToken = default);
    Task<ProcedureResultDto?> InsertKeepAliveAsync(KeepAliveRequest request, CancellationToken cancellationToken = default);
    Task<ProcedureResultDto?> InsertAtmAsync(InsertAtmRequest request, CancellationToken cancellationToken = default);
    Task<ProcedureResultDto?> InsertAtmContadoresAsync(InsertAtmContadoresRequest request, CancellationToken cancellationToken = default);
    Task<string?> ComandoAtmAsync(string idCajero, CancellationToken cancellationToken = default);
    Task<string?> InsertDispositivoAsync(InsertDispositivoRequest request, CancellationToken cancellationToken = default);
    Task<string?> InsertDispositivoAsyncTipo(InsertDispositivoRequestM request, CancellationToken cancellationToken = default);
    Task<string?> InsertTransaccionAsync(TransaccionRequest request, CancellationToken cancellationToken = default);
    Task<string?> GetTransaccionesAsync(PostTransaccion request, CancellationToken cancellationToken = default);
    Task<IEnumerable<FallaEPResponse>> GetTotalEPAsync(FallaEPRequest request, CancellationToken cancellationToken = default);
    Task<DashboardResponseEP> GetEpDashboardMonitorAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<EPDatosResponse>> GetTotalEPDatosAsync(EPRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<EPContadoresResponse>> GetTotalEPContadoresAsync(EPRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<EstatusEquipoEPResponse>> GetEstatusEEPAsync(EPRequest request, CancellationToken cancellationToken = default);
    Task<AgenteComunicacionEpResponse?> GetAgenteComunicacionEpAsync(EPRequest request, CancellationToken cancellationToken = default);
    Task<string?> GetTransaccionesEpAsync(PostTransaccionEp request, CancellationToken cancellationToken = default);
    Task<IEnumerable<CardsDash>> GetCardsDashAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CatalogoEquipoRow>> GetCatalogoEquiposAsync(CatalogoEquiposRequest request, CancellationToken cancellationToken = default);

    Task<CatalogoEquiposResumenRegionResponse> GetCatalogoEquiposResumenRegionAsync(CancellationToken cancellationToken = default);
    Task<EquipoDetalleMvcResponse?> GetEquipoDetalleMvcAsync(EquipoDetalleMvcRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Dictionary<string, string>>> GetRolloutReportAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReporteCatalogRow>> GetReportesCatalogAsync(CancellationToken cancellationToken = default);
    Task<PrepararSolicitudReporteResponse> PrepararSolicitudReporteAsync(PrepararSolicitudReporteRequest request, CancellationToken cancellationToken = default);

    Task<int?> InsertReporteHistoricoAsync(
        string nombreArchivo,
        string extension,
        string ruta,
        string usuario,
        int idReporte,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReporteHistoricoRowDto>> GetReportesHistoricosAsync(
        ReportesHistoricosRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Dictionary<string, string>>> GetReporteContadoresAsync(
        ReporteContadoresRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Dictionary<string, string>>> GetReporteExportPaginadoAsync(
        ReporteExportRequest request,
        CancellationToken cancellationToken = default);

    Task<ProcedureResultDto?> StartDetalleEquipoSesionAsync(DetalleEquipoSesionRequest request, CancellationToken cancellationToken = default);
    Task<ProcedureResultDto?> PingDetalleEquipoSesionAsync(DetalleEquipoPingRequest request, CancellationToken cancellationToken = default);
    Task<ProcedureResultDto?> StopDetalleEquipoSesionAsync(DetalleEquipoStopRequest request, CancellationToken cancellationToken = default);
    Task<ProcedureResultDto?> InsertDetalleEquipoPerfAsync(PerfMetricaInsertRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PerfMetricaPunto>> GetDetalleEquipoPerfSerieAsync(PerfMetricaSerieRequest request, CancellationToken cancellationToken = default);

    Task<ProcedureResultDto?> UpsertDetalleEquipoSoDdFromAgentAsync(
        DetalleEquipoSoSyncSubmitRequest request,
        CancellationToken cancellationToken = default);
}
