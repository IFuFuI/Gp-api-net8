using System.Text.Json.Serialization;

namespace ATT.Monitor.Api.Models.Dashboard;

#region Fallas / catálogo / equipo MVC

public sealed class FallaRequest
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string? Buscar { get; set; }
    public string OrderBy { get; set; } = "Id_ATM";
    public string OrderDir { get; set; } = "DESC";
    public int ID_DISPO { get; set; }
    public string FECHA_INI { get; set; } = string.Empty;

    [JsonPropertyName("FECHA_FIN")]
    public string Fecha_FIN { get; set; } = string.Empty;
}

public sealed class FallaResponse
{
    public string Id_ATM { get; set; } = string.Empty;
    public int ID_PB_TIENDA { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Adress { get; set; } = string.Empty;
    public string REGION { get; set; } = string.Empty;
    public string Location_Name { get; set; } = string.Empty;
    public string DISPOSITIVO { get; set; } = string.Empty;
    public string SUBCATEGORIA { get; set; } = string.Empty;
    public string Fecha_Ini { get; set; } = string.Empty;
    public string? Fecha_FIN { get; set; }
    public int TOTAL_FILTRADOS { get; set; }
}

public sealed class CatalogoEquiposRequest
{
    public int Ignorar { get; set; }
    public int CantidadFila { get; set; }
    public string? Filtro { get; set; }
    /// <summary>Región exacta desde KPI; null o vacío = todos los cajeros.</summary>
    public string? RegionFiltro { get; set; }
    public string Orden { get; set; } = "ID_ESTACION_PAGO";
    public string Dir { get; set; } = "ASC";
}

public sealed class CatalogoEquiposRegionKpiRow
{
    public string Region { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

public sealed class CatalogoEquiposResumenRegionResponse
{
    public int TotalCajeros { get; set; }
    public IReadOnlyList<CatalogoEquiposRegionKpiRow> PorRegion { get; init; } = Array.Empty<CatalogoEquiposRegionKpiRow>();
}

public sealed class CatalogoEquipoRow
{
    public int ID { get; set; }
    public string ID_ESTACION_PAGO { get; set; } = string.Empty;
    public string NOMBRE_TIENDA { get; set; } = string.Empty;
    public string REGION { get; set; } = string.Empty;
    public string ID_PB_TIENDA { get; set; } = string.Empty;
    public string ESTADO { get; set; } = string.Empty;
    public string MODELO { get; set; } = string.Empty;
    public string ESTATUS { get; set; } = string.Empty;
    public int TOTAL { get; set; }
}

public sealed class EquipoDetalleMvcRequest
{
    public string Id { get; set; } = string.Empty;
}

public sealed class EquipoInfoMvcDto
{
    public string? INFORMACION_DE_EQUIPO { get; set; }
    public int? ID_EP { get; set; }
    public string? REGION { get; set; }
    public int? ID_PB { get; set; }
    public string? MODELO { get; set; }
    public string? SUCURSAL { get; set; }
    public string? DIRECCION { get; set; }
    public string? SERIE { get; set; }
    public int? ID_NEO { get; set; }
    public string? NOMBRE_EQUIPO { get; set; }
    public string? IP { get; set; }
    public string? LOCALIDAD { get; set; }
    public string? VERSION_APLICATIVO { get; set; }
    public string? VERSION_INSTALACION { get; set; }
    public string? CAJA { get; set; }
    public string? USUARIO { get; set; }
    public string? PASSWORD { get; set; }
    public string? MACADDRESS { get; set; }
    public string? CUENTA { get; set; }
    public string? VPN_USUARIO { get; set; }
    public string? VPN_PASSWORD { get; set; }
    public string? CASS1 { get; set; }
    public string? CASS2 { get; set; }
    public string? URL_RECARGA_TA { get; set; }
    public string? URL_OTROS_SERVICIOS { get; set; }
    public string? URL_PAGO_FACTURA { get; set; }
    public string? CC_CODI { get; set; }
    public string? CC_EFE { get; set; }
    public string? CC_VD1 { get; set; }
    public string? STATUS_CAJA { get; set; }
    public string? URL_BRANCH { get; set; }
}

public sealed class EquipoSoMvcDto
{
    public string? SISTEMA_OPERATIVO { get; set; }
    public double MEMORIA_RAM { get; set; }
    public string? IDIOMA { get; set; }
    public string? RESOLUCION { get; set; }
    public string? PROCESADOR { get; set; }
    public string? NOMBRE_DE_WINDOWS { get; set; }
    public string? ZONA_HORARIA { get; set; }
    public string? NOMBRE_EQUIPO { get; set; }
}

public sealed class EquipoDiscoMvcDto
{
    public string? UNIDAD_DISCO_DURO { get; set; }
    public string? TAMANHO_DISCO_DURO { get; set; }
    public string? ESPACIO_LIBRE_DD { get; set; }
}

public sealed class EquipoEstatusSoftwareMvcDto
{
    public string? ESTATUS_SOFTWARE { get; set; }
    public string? ACEPTADOR_BILLETES { get; set; }
    public string? DISPENSADOR_BILLETES { get; set; }
    public string? IMPRESORA { get; set; }
    public string? LECTOR_CODIGO_DE_BARRAS { get; set; }
    public string? LECTOR_TARJETAS { get; set; }
    public string? ULTIMO_CAMBIO_ESTATUS { get; set; }
}

public sealed class EquipoDetalleMvcResponse
{
    public EquipoInfoMvcDto? Informacion { get; set; }
    public EquipoSoMvcDto? SistemaOperativo { get; set; }
    public EquipoDiscoMvcDto? DiscoDuro { get; set; }
    public EquipoEstatusSoftwareMvcDto? EstatusSoftware { get; set; }
}

#endregion

#region Cards / keep-alive / ATM insert / dispositivos

public sealed class MCards
{
    public int? Total { get; set; }
    public int? Sin_Efectivo { get; set; }
    public int? Falla_Aceptador { get; set; }
    public int? Falla_Dispensador { get; set; }
    public int? Falla_Impresora { get; set; }
    public int? Falla_Comunicacion { get; set; }
}

public sealed class CardsDash
{
    public string Dispositivo { get; set; } = string.Empty;
    public string Total { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
}

public sealed class KeepAliveRequest
{
    public string KeepAlive { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string IdCajero { get; set; } = string.Empty;
}

public sealed class InsertAtmRequest
{
    public string NumeroCajero { get; set; } = string.Empty;
    public string Sucursal { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string AdministradoPor { get; set; } = string.Empty;
    public string Zona { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string Canal { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string FechaHora { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
}

public sealed class InsertDispositivoRequest
{
    public string IdCajero { get; set; } = string.Empty;
    public string NombreDispositivo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class DispositivoData
{
    public string Tipo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class InsertDispositivoRequestM
{
    public string IdCajero { get; set; } = string.Empty;
    public string NombreDispositivo { get; set; } = string.Empty;
    public List<DispositivoData> Data { get; set; } = [];
}

public sealed class InsertAtmContadoresRequest
{
    public string IdCajero { get; set; } = string.Empty;
    public int? IniC1 { get; set; }
    public int? IniC2 { get; set; }
    public int? IniC3 { get; set; }
    public int? IniC4 { get; set; }
    public int? IniC5 { get; set; }
    public int? IniC6 { get; set; }
    public int? RemC1 { get; set; }
    public int? RemC2 { get; set; }
    public int? RemC3 { get; set; }
    public int? RemC4 { get; set; }
    public int? RemC5 { get; set; }
    public int? RemC6 { get; set; }
    public int? DispC1 { get; set; }
    public int? DispC2 { get; set; }
    public int? DispC3 { get; set; }
    public int? DispC4 { get; set; }
    public int? DispC5 { get; set; }
    public int? DispC6 { get; set; }
    public int? RechC1 { get; set; }
    public int? RechC2 { get; set; }
    public int? RechC3 { get; set; }
    public int? RechC4 { get; set; }
    public int? RechC5 { get; set; }
    public int? RechC6 { get; set; }
    public int? CDOM_C1 { get; set; }
    public int? CDOM_C2 { get; set; }
    public int? CDOM_C3 { get; set; }
    public int? CDOM_C4 { get; set; }
    public int? CDOM_C5 { get; set; }
    public int? CDOM_C6 { get; set; }
}

#endregion

#region Transacciones / EP / reportes

public sealed class TransaccionRequest
{
    public int IDTRANSACCION { get; set; }
    public string EP { get; set; } = " ";
    public string FECHA { get; set; } = " ";
    public string TIPO { get; set; } = " ";
    public string ESTATUS { get; set; } = " ";
    public string TipOPAGO { get; set; } = " ";
    public string CLIENTE { get; set; } = " ";
    public string NombrECLIENTE { get; set; } = " ";
    public decimal? MONTO { get; set; }
    public decimal? CAMBIO { get; set; }
    public string FOLIO { get; set; } = " ";
    public string CAMBIO_INCOMPLETO { get; set; } = " ";
    public string CodigOERROR { get; set; } = " ";
    public string MOTIVO_RECHAZO { get; set; } = " ";
    public string DN { get; set; } = " ";
    public string CODIGO_AUTORIZACION { get; set; } = " ";
    public string NO_TARJETA { get; set; } = " ";
    public string REFERENCIA { get; set; } = " ";
    public string REFERENCIA_EP { get; set; } = " ";
    public string COLOR { get; set; } = " ";
    public string JOURNAL { get; set; } = " ";
    public string? FECHA_REGISTRO { get; set; } = " ";
    public string Operacion { get; set; } = " ";
}

public sealed class PostTransaccion
{
    public int Ignorar { get; set; }
    public int Cantidad_Fila { get; set; }
    public string Filtro { get; set; } = string.Empty;
    public string Orden { get; set; } = string.Empty;
    public string Dir { get; set; } = string.Empty;
}

/// <summary>Transacciones de un EP en rango de fechas (<c>SP_GET_TRANSACCIONES_EP</c>).</summary>
public sealed class PostTransaccionEp
{
    public string EP { get; set; } = string.Empty;
    public string FechaDesde { get; set; } = string.Empty;
    public string FechaHasta { get; set; } = string.Empty;
    public int Ignorar { get; set; }
    public int Cantidad_Fila { get; set; } = 25;
    public string Orden { get; set; } = "FECHA";
    public string Dir { get; set; } = "desc";
}

public sealed class AgenteComunicacionEpResponse
{
    public string? IdAtm { get; set; }
    public string? KeepAlive { get; set; }
    public string? Status { get; set; }
    public DateTime? FechaUltimaModificacion { get; set; }
    public bool EnLinea { get; set; }
    public string? Detalle { get; set; }
}

public sealed class FallaEPRequest
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string? Buscar { get; set; }
    public string OrderBy { get; set; } = "Id_ATM";
    public string OrderDir { get; set; } = "DESC";
}

public sealed class FallaEPResponse
{
    public string ID_ATM { get; set; } = string.Empty;
    public string ESTADO { get; set; } = string.Empty;
    public int? TOTAL_FALLAS { get; set; }
    public DateTime? FECHA { get; set; }
    public string TIPO { get; set; } = string.Empty;
    public int? DISPOSITIVO { get; set; }
    public string SUCURSAL { get; set; } = string.Empty;
    public string NOMBRE { get; set; } = string.Empty;
    public string CIUDAD { get; set; } = string.Empty;
    public string ESTADO_ATM { get; set; } = string.Empty;
    public string PLAZA { get; set; } = string.Empty;
    public string ZONA { get; set; } = string.Empty;
    public string MODELO { get; set; } = string.Empty;
    public string ADMINISTRADO_POR { get; set; } = string.Empty;
    public string ETV { get; set; } = string.Empty;
    public string ULTIMO_STATUS { get; set; } = string.Empty;
    public int RN { get; set; }
    public int TOTAL_REGISTROS { get; set; }
}

public sealed class EPRequest
{
    public string EP { get; set; } = string.Empty;
}

public sealed class DetalleEquipoSesionRequest
{
    public Guid IdSesion { get; set; }
    public string IdAtm { get; set; } = string.Empty;
    public string? Usuario { get; set; }
}

public sealed class DetalleEquipoStopRequest
{
    public Guid IdSesion { get; set; }
}

public sealed class DetalleEquipoPingRequest
{
    public Guid IdSesion { get; set; }
}

public sealed class PerfMetricaInsertRequest
{
    public Guid? IdSesion { get; set; }
    public string IdAtm { get; set; } = string.Empty;
    public decimal CpuPct { get; set; }
    public decimal RamPct { get; set; }
    public decimal? RamUsadaMb { get; set; }
    public decimal? RamTotalMb { get; set; }
}

public sealed class PerfMetricaSerieRequest
{
    public string IdAtm { get; set; } = string.Empty;
    public int Minutos { get; set; } = 5;
    public int BucketSeg { get; set; } = 5;
}

public sealed class PerfMetricaPunto
{
    public DateTime Bucket_Utc { get; set; }
    public decimal Cpu_Pct { get; set; }
    public decimal Ram_Pct { get; set; }
    public decimal? Ram_Usada_Mb { get; set; }
    public decimal? Ram_Total_Mb { get; set; }
}

public sealed class DetalleEquipoSoSyncRequest
{
    public Guid IdSesion { get; set; }
    public string IdAtm { get; set; } = string.Empty;
}

public sealed class DetalleEquipoSoSyncStatusQuery
{
    public Guid IdSesion { get; set; }
    public string IdAtm { get; set; } = string.Empty;
}

public sealed class DetalleEquipoSoSyncPollRequest
{
    public string IdAtm { get; set; } = string.Empty;
}

public sealed class DetalleEquipoSoSyncPollResponse
{
    public bool NeedsSync { get; set; }
    public Guid? IdSesion { get; set; }
}

public sealed class DetalleEquipoSoSyncSubmitRequest
{
    public Guid IdSesion { get; set; }
    public string IdAtm { get; set; } = string.Empty;
    public string? SistemaOperativo { get; set; }
    public double MemoriaRamGb { get; set; }
    public string? Idioma { get; set; }
    public string? Resolucion { get; set; }
    public string? Procesador { get; set; }
    public string? NombreDeWindows { get; set; }
    public string? ZonaHoraria { get; set; }
    public string? NombreEquipoSo { get; set; }
    public string? UnidadDiscoDuro { get; set; }
    public string? TamanhoDiscoDuro { get; set; }
    public string? EspacioLibreDd { get; set; }
}

public sealed class DetalleEquipoSoSyncRequestAck
{
    public string Kind { get; set; } = string.Empty;
}

public sealed class DetalleEquipoSoSyncStatusResponse
{
    public string State { get; set; } = string.Empty;
}

public sealed class ResponseDetalleMonitor
{
    public int EP_Monitoreados { get; set; }
    public int EP_Activos { get; set; }
    public int EP_Con_Fallas { get; set; }
    public string ULTIMA_ACTUALIZACION { get; set; } = string.Empty;
}

public sealed class UbicacionResponse
{
    public string Ubicacion { get; set; } = string.Empty;

    /// <summary>EP activas en el estado (clave <c>ubicacion</c> / hc-key).</summary>
    public int EP_Total { get; set; }

    public int EP_Con_Falla { get; set; }
    public int TotalFallas { get; set; }
    public string estado { get; set; } = string.Empty;

    /// <summary>Opcional: si el SP añade columnas, el mapa Blazor las usa antes que el catálogo estático.</summary>
    public double? Lat { get; set; }

    public double? Lng { get; set; }
}

public sealed class DashboardResponseEP
{
    public IEnumerable<UbicacionResponse> Mapa { get; set; } = [];
    public ResponseDetalleMonitor Estadisticas { get; set; } = new();
}

public sealed class EPDatosResponse
{
    public string ID_EP { get; set; } = string.Empty;
    public string SUCURSAL { get; set; } = string.Empty;
    public string NOMBRE { get; set; } = string.Empty;
    public string DIRECCION { get; set; } = string.Empty;
    public string ESTADO { get; set; } = string.Empty;
    public string ADMINISTRADO_POR { get; set; } = string.Empty;
    public string ZONA { get; set; } = string.Empty;
    public string MODEL { get; set; } = string.Empty;
    public string SERIE { get; set; } = string.Empty;
    public string CANAL { get; set; } = string.Empty;
    public string TIPO { get; set; } = string.Empty;
}

public sealed class ContadoresIniciales
{
    public int INI_C1 { get; set; }
    public int INI_C2 { get; set; }
    public int INI_C3 { get; set; }
    public int INI_C4 { get; set; }
}

public sealed class ContadoresRemanente
{
    public int REM_C1 { get; set; }
    public int REM_C2 { get; set; }
    public int REM_C3 { get; set; }
    public int REM_C4 { get; set; }
}

public sealed class ContadoresDispensados
{
    public int DISP_C1 { get; set; }
    public int DISP_C2 { get; set; }
    public int DISP_C3 { get; set; }
    public int DISP_C4 { get; set; }
}

public sealed class ContadoresRechazos
{
    public int RECH_C1 { get; set; }
    public int RECH_C2 { get; set; }
    public int RECH_C3 { get; set; }
    public int RECH_C4 { get; set; }
}

public sealed class EPContadoresResponse
{
    public int TOTAL_REMANENTE { get; set; }
    public ContadoresIniciales Iniciales { get; set; } = new();
    public ContadoresRemanente Remanente { get; set; } = new();
    public ContadoresDispensados Dispensados { get; set; } = new();
    public ContadoresRechazos Rechazos { get; set; } = new();
}

public sealed class EstatusEquipoEPResponse
{
    public string DISPOSITIVO { get; set; } = string.Empty;
    public string NOMBRE_DISPOSITIVO { get; set; } = string.Empty;
    public string ESTADO_DESCRIPCION { get; set; } = string.Empty;
}

public sealed class RolloutReportRequest { }

public sealed class ReportesCatalogRequest { }

public sealed class ReporteCatalogRow
{
    public int ID { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public sealed class PrepararSolicitudReporteRequest
{
    public int IdReporte { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Formato { get; set; } = "csv";
}

public sealed class PrepararSolicitudReporteResponse
{
    public bool Valido { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string? DescripcionReporte { get; set; }
    public int RangoDias { get; set; }
    public string? FormatoNormalizado { get; set; }
}

/// <summary>Generación de archivo en servidor + registro en <c>BD_REPORTES_HISTORICOS</c>.</summary>
public sealed class GenerarReporteRequest
{
    public int IdReporte { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Formato { get; set; } = "csv";
    /// <summary>Filtro opcional para reporte «Transacciones por Equipo» (id 3).</summary>
    public string? Ep { get; set; }
    /// <summary>Reporte #3: una o varias EP (vacío = todas).</summary>
    public IReadOnlyList<string>? Eps { get; set; }
}

public sealed class GenerarReporteResponse
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int? IdReportesHistorico { get; set; }
    public string? NombreArchivo { get; set; }
    public int FilasExportadas { get; set; }
}

/// <summary>Paginación genérica para SP <c>dbo.SP_GET_REPORTE_*</c> (exportación bitácora).</summary>
public sealed class ReporteExportRequest
{
    public string StoredProcedure { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int Ignorar { get; set; }
    public int CantidadFila { get; set; } = 500;
    public string? Filtro { get; set; }
    public string Orden { get; set; } = "FECHA";
    public string Dir { get; set; } = "ASC";
    public bool? SoloError { get; set; }
    public string? Ep { get; set; }
    /// <summary>Lista CSV de EP para reporte transacciones por equipo.</summary>
    public string? EpsCsv { get; set; }
}

/// <summary>Solicitud para <c>dbo.SP_GET_REPORTEDECONTADORES</c> (paridad API v1).</summary>
public sealed class ReporteContadoresRequest
{
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int Ignorar { get; set; }
    public int CantidadFila { get; set; } = 100;
    public string? Filtro { get; set; }
    public string Orden { get; set; } = "ID";
    public string Dir { get; set; } = "ASC";
}

/// <summary>Solicitud para <c>dbo.SP_GET_Reportes_Historicos</c> (paridad API v1).</summary>
public sealed class ReportesHistoricosRequest
{
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int Ignorar { get; set; }
    public int CantidadFila { get; set; } = 100;
    public string? Filtro { get; set; }
    public string Orden { get; set; } = "FECHA_CREACION";
    public string Dir { get; set; } = "desc";
}

/// <summary>Fila de reportes históricos (paridad API v1).</summary>
public sealed class ReporteHistoricoRowDto
{
    public int IdReportesHistorico { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public int IdReporte { get; set; }
    public DateTime FechaCreacion { get; set; }
}

#endregion
