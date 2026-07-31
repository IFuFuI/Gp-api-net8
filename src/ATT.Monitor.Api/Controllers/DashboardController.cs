using System.Security.Claims;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Infrastructure.DetalleEquipo;
using ATT.Monitor.Api.Models.Dashboard;
using ATT.Monitor.Api.Models.Login;
using ATT.Monitor.Api.Models.TransArchivo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>Paridad <c>api/Dashboard</c> → <c>api/Dashboard</c>.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DashboardController(
    IDashboardData dashboard,
    ITransArchivoData transArchivo,
    IReporteHistoricoDescargaService reporteHistoricoDescarga,
    IReporteGeneracionService reporteGeneracion,
    IDetalleEquipoSoSyncCoordinator soSync) : ControllerBase
{
    [HttpPost("GET_CARDS_DASHBOARD")]
    [ProducesResponseType(typeof(MCards), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCardsDashboard()
    {
        var result = await dashboard.GetCardsAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("GET_FALLAS")]
    [ProducesResponseType(typeof(IEnumerable<FallaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFallasAsync([FromBody] FallaRequest request)
    {
        var result = await dashboard.GetFallasAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("POST_KEEP_ALIVE")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostKeepAlive([FromBody] KeepAliveRequest request)
    {
        var result = await dashboard.InsertKeepAliveAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("GET_TOTAL_EP")]
    [ProducesResponseType(typeof(IEnumerable<FallaEPResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTotalEPAsync([FromBody] FallaEPRequest request)
    {
        var result = await dashboard.GetTotalEPAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("GET_TOTAL_EP_DATOS")]
    [ProducesResponseType(typeof(IEnumerable<EPDatosResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTotalEPDatosAsync([FromBody] EPRequest request)
    {
        var result = await dashboard.GetTotalEPDatosAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("GET_TOTAL_EP_CONTADORES")]
    [ProducesResponseType(typeof(IEnumerable<EPContadoresResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTotalEPContadoresAsync([FromBody] EPRequest request)
    {
        var result = await dashboard.GetTotalEPContadoresAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("GET_ESTATUS_EQUIPO_EP")]
    [ProducesResponseType(typeof(IEnumerable<EstatusEquipoEPResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEstatusEEPAsync([FromBody] EPRequest request)
    {
        var result = await dashboard.GetEstatusEEPAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("GET_AGENTE_COMUNICACION_EP")]
    [ProducesResponseType(typeof(AgenteComunicacionEpResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAgenteComunicacionEpAsync([FromBody] EPRequest request)
    {
        var result = await dashboard.GetAgenteComunicacionEpAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result ?? new AgenteComunicacionEpResponse { Detalle = "EP no especificado.", EnLinea = false });
    }

    [HttpPost("GET_CATALOGO_EQUIPOS")]
    [ProducesResponseType(typeof(IEnumerable<CatalogoEquipoRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalogoEquiposAsync([FromBody] CatalogoEquiposRequest request)
    {
        var result = await dashboard.GetCatalogoEquiposAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("GET_CATALOGO_EQUIPOS_RESUMEN_REGION")]
    [ProducesResponseType(typeof(CatalogoEquiposResumenRegionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalogoEquiposResumenRegionAsync()
    {
        var result = await dashboard.GetCatalogoEquiposResumenRegionAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("GET_EQUIPOS_DETALLE_MVC")]
    [ProducesResponseType(typeof(EquipoDetalleMvcResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEquipoDetalleMvcAsync([FromBody] EquipoDetalleMvcRequest request)
    {
        var result = await dashboard.GetEquipoDetalleMvcAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result ?? new EquipoDetalleMvcResponse());
    }

    [HttpPost("DETALLE_EQUIPO_START")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> StartDetalleEquipoAsync([FromBody] DetalleEquipoSesionRequest request)
    {
        var result = await dashboard.StartDetalleEquipoSesionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result ?? new ProcedureResultDto { ResultInt = 1, ResultString = "OK" });
    }

    [HttpPost("DETALLE_EQUIPO_PING")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PingDetalleEquipoAsync([FromBody] DetalleEquipoPingRequest request)
    {
        var result = await dashboard.PingDetalleEquipoSesionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result ?? new ProcedureResultDto { ResultInt = 1, ResultString = "OK" });
    }

    [HttpPost("DETALLE_EQUIPO_STOP")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> StopDetalleEquipoAsync([FromBody] DetalleEquipoStopRequest request)
    {
        soSync.AbandonSession(request.IdSesion);
        var result = await dashboard.StopDetalleEquipoSesionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result ?? new ProcedureResultDto { ResultInt = 1, ResultString = "OK" });
    }

    [HttpPost("DETALLE_EQUIPO_INSERT_PERF")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> InsertDetalleEquipoPerfAsync([FromBody] PerfMetricaInsertRequest request)
    {
        var result = await dashboard.InsertDetalleEquipoPerfAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result ?? new ProcedureResultDto { ResultInt = 1, ResultString = "OK" });
    }

    [HttpPost("DETALLE_EQUIPO_GET_PERF_SERIE")]
    [ProducesResponseType(typeof(IEnumerable<PerfMetricaPunto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetalleEquipoPerfSerieAsync([FromBody] PerfMetricaSerieRequest request)
    {
        var result = await dashboard.GetDetalleEquipoPerfSerieAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Monitor web: encola una lectura SO/DD desde el cajero (una por sesión de detalle).</summary>
    [HttpPost("DETALLE_EQUIPO_SO_SYNC_REQUEST")]
    [ProducesResponseType(typeof(DetalleEquipoSoSyncRequestAck), StatusCodes.Status200OK)]
    public IActionResult RequestDetalleSoSync([FromBody] DetalleEquipoSoSyncRequest request)
    {
        var r = soSync.RegisterRequest(request.IdAtm, request.IdSesion);
        var kind = r.Kind switch
        {
            DetalleEquipoSoSyncRegisterKind.AlreadyCompleted => "completed",
            _ => "queued"
        };
        return Ok(new DetalleEquipoSoSyncRequestAck { Kind = kind });
    }

    [HttpPost("DETALLE_EQUIPO_SO_SYNC_STATUS")]
    [ProducesResponseType(typeof(DetalleEquipoSoSyncStatusResponse), StatusCodes.Status200OK)]
    public IActionResult GetDetalleSoSyncStatus([FromBody] DetalleEquipoSoSyncStatusQuery request)
    {
        var st = soSync.GetStatus(request.IdAtm, request.IdSesion);
        var state = st.State switch
        {
            DetalleEquipoSoSyncUiState.Pending => "pending",
            DetalleEquipoSoSyncUiState.InFlight => "inflight",
            DetalleEquipoSoSyncUiState.Completed => "completed",
            _ => "unknown"
        };
        return Ok(new DetalleEquipoSoSyncStatusResponse { State = state });
    }

    /// <summary>Agente FileReadingATT: ¿hay trabajo pendiente para este IdCajero?</summary>
    [HttpPost("DETALLE_EQUIPO_SO_SYNC_POLL")]
    [ProducesResponseType(typeof(DetalleEquipoSoSyncPollResponse), StatusCodes.Status200OK)]
    public IActionResult PollDetalleSoSync([FromBody] DetalleEquipoSoSyncPollRequest request)
    {
        var next = soSync.TryClaimNext(request.IdAtm);
        if (next is null)
            return Ok(new DetalleEquipoSoSyncPollResponse { NeedsSync = false });
        return Ok(new DetalleEquipoSoSyncPollResponse { NeedsSync = true, IdSesion = next.Value.IdSesion });
    }

    /// <summary>Agente: envía snapshot SO/DD tras <see cref="PollDetalleSoSync"/>.</summary>
    [HttpPost("DETALLE_EQUIPO_SO_SYNC_SUBMIT")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitDetalleSoSyncAsync([FromBody] DetalleEquipoSoSyncSubmitRequest body)
    {
        var st = soSync.GetStatus(body.IdAtm, body.IdSesion);
        if (st.State != DetalleEquipoSoSyncUiState.InFlight)
            return BadRequest(new { error = "Sesión no en estado in-flight; reintente poll o solicitud." });

        try
        {
            var result = await dashboard.UpsertDetalleEquipoSoDdFromAgentAsync(body, HttpContext.RequestAborted).ConfigureAwait(false);
            soSync.MarkSubmitSuccess(body.IdAtm, body.IdSesion);
            return Ok(result ?? new ProcedureResultDto { ResultInt = 1, ResultString = "OK" });
        }
        catch
        {
            soSync.MarkSubmitFailure(body.IdAtm, body.IdSesion);
            throw;
        }
    }

    [HttpPost("GET_ROLLOUT_REPORTE")]
    [ProducesResponseType(typeof(IEnumerable<Dictionary<string, string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolloutReportAsync([FromBody] RolloutReportRequest? _)
    {
        var result = await dashboard.GetRolloutReportAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("GET_CATALOGO_REPORTES")]
    [ProducesResponseType(typeof(IEnumerable<ReporteCatalogRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalogoReportesAsync([FromBody] ReportesCatalogRequest? _)
    {
        var result = await dashboard.GetReportesCatalogAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("PREPARAR_SOLICITUD_REPORTE")]
    [ProducesResponseType(typeof(PrepararSolicitudReporteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PrepararSolicitudReporteResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PrepararSolicitudReporteAsync([FromBody] PrepararSolicitudReporteRequest? request)
    {
        if (request is null)
        {
            return BadRequest(new PrepararSolicitudReporteResponse
            {
                Valido = false,
                Mensaje = "Cuerpo JSON requerido."
            });
        }

        var result = await dashboard.PrepararSolicitudReporteAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        if (!result.Valido)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("GENERAR_REPORTE")]
    [ProducesResponseType(typeof(GenerarReporteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenerarReporteResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerarReporteAsync([FromBody] GenerarReporteRequest? request)
    {
        if (request is null)
        {
            return BadRequest(new GenerarReporteResponse
            {
                Exito = false,
                Mensaje = "Cuerpo JSON requerido."
            });
        }

        var usuario = User.FindFirstValue(ClaimTypes.Name)
                        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.Identity?.Name
                        ?? "monitor";

        var result = await reporteGeneracion.GenerarAsync(request, usuario, HttpContext.RequestAborted)
            .ConfigureAwait(false);
        if (!result.Exito)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("GET_REPORTES_HISTORICOS")]
    [ProducesResponseType(typeof(IEnumerable<ReporteHistoricoRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetReportesHistoricosAsync([FromBody] ReportesHistoricosRequest? request)
    {
        if (request is null)
            return BadRequest();

        var rows = await dashboard.GetReportesHistoricosAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(rows);
    }

    [HttpPost("GET_REPORTE_CONTADORES")]
    [ProducesResponseType(typeof(IEnumerable<Dictionary<string, string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetReporteContadoresAsync([FromBody] ReporteContadoresRequest? request)
    {
        if (request is null)
            return BadRequest();

        if (request.FechaFin.Date < request.FechaInicio.Date)
            return BadRequest("La fecha fin no puede ser anterior a la fecha de inicio.");

        var rows = await dashboard.GetReporteContadoresAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(rows);
    }

    [HttpGet("DOWNLOAD_REPORTE_HISTORICO_FILE/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadReporteHistoricoFile([FromRoute] int id)
    {
        var open = await reporteHistoricoDescarga.TryOpenHistoricoAsync(id, HttpContext.RequestAborted).ConfigureAwait(false);
        if (open is null)
            return NotFound();

        return File(open.Stream, open.ContentType, open.DownloadFileName);
    }

    [HttpGet("GET_CARDS_DASH")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CardsDash>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCardsDash()
    {
        var result = await dashboard.GetCardsDashAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        if (result is null || !result.Any())
            return NoContent();
        return Ok(result);
    }

    [HttpGet("GET_DASH_MONITOR_EP")]
    [ProducesResponseType(typeof(DashboardResponseEP), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardAsync()
    {
        var result = await dashboard.GetEpDashboardMonitorAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("POST_ATM")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PostAtm([FromBody] InsertAtmRequest atm)
    {
        var result = await dashboard.InsertAtmAsync(atm, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("POST_ATM_CONTADORES")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PostAtmContadores([FromBody] InsertAtmContadoresRequest request)
    {
        var result = await dashboard.InsertAtmContadoresAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("GET_COMANDO_ATM")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComandoAtm([FromQuery] string idcajero)
    {
        var result = await dashboard.ComandoAtmAsync(idcajero, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("INSERT_STATUS_DISPOSITIVOS")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> InsertStatusDispositivos([FromBody] InsertDispositivoRequest request)
    {
        var result = await dashboard.InsertDispositivoAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("INSERT_STATUS_DISPOSITIVOS_TIPO")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> InsertDispositivoAsyncTipo([FromBody] InsertDispositivoRequestM request)
    {
        var result = await dashboard.InsertDispositivoAsyncTipo(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("INSERT_TRANSACCIONES")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> InsertTransacciones([FromBody] TransaccionRequest request)
    {
        var result = await dashboard.InsertTransaccionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("GET_PACKAGE")]
    [ProducesResponseType(typeof(MArchivo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArchivos([FromQuery] string idcajero)
    {
        if (string.IsNullOrWhiteSpace(idcajero))
            return BadRequest("Parámetro 'idcajero' requerido.");

        var paquete = await transArchivo.GetPaqueteAsync(idcajero, HttpContext.RequestAborted).ConfigureAwait(false);
        if (paquete is null)
            return NotFound("No se encontró información para el cajero especificado.");
        return Ok(paquete);
    }

    [HttpGet("GET_PRUEBA")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult Prueba() => Ok("IDC_RESET");
}
