namespace ATT.Monitor.Api.Models.Historial;

/// <summary>Respuesta ligera de <c>GET api/Historial/resumen</c> (solo metadatos sobre reportes históricos).</summary>
public sealed class HistorialResumenResponse
{
    public int DiasVentana { get; set; }
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public int ReportesHistoricosEnVentana { get; set; }
    public string? UltimoNombreArchivo { get; set; }
    public DateTime? UltimaFechaCreacion { get; set; }
}
