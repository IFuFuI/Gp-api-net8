using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Dashboard;
using ATT.Monitor.Api.Models.Historial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class HistorialController(IDashboardData dashboard) : ControllerBase
{
    /// <summary>Metadatos ligeros sobre reportes históricos en una ventana reciente (sin agregar SP).</summary>
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(HistorialResumenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResumenAsync([FromQuery] int dias = 30, CancellationToken cancellationToken = default)
    {
        dias = Math.Clamp(dias, 1, 365);
        var end = DateTime.UtcNow.Date;
        var start = end.AddDays(-dias);

        var request = new ReportesHistoricosRequest
        {
            FechaInicio = start,
            FechaFin = end,
            Ignorar = 0,
            CantidadFila = 2000,
            Filtro = null,
            Orden = "FECHA_CREACION",
            Dir = "desc"
        };

        var rows = await dashboard.GetReportesHistoricosAsync(request, cancellationToken).ConfigureAwait(false);
        var ordered = rows.OrderByDescending(r => r.FechaCreacion).ToList();
        var first = ordered.FirstOrDefault();

        return Ok(new HistorialResumenResponse
        {
            DiasVentana = dias,
            FechaDesde = start,
            FechaHasta = end,
            ReportesHistoricosEnVentana = rows.Count,
            UltimoNombreArchivo = first?.NombreArchivo,
            UltimaFechaCreacion = first?.FechaCreacion
        });
    }
}
