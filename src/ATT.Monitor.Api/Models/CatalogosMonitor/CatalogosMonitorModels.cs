namespace ATT.Monitor.Api.Models.CatalogosMonitor;

/// <summary>Listado paginado (paginación 1-based, alineado con <c>AceptadorRequest</c> en administración).</summary>
public sealed class MonitorCatalogListRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Buscar { get; set; }
    public string OrderBy { get; set; } = "ID";
    public string OrderDir { get; set; } = "ASC";
}

public sealed class MonitorCatalogPageResult<T>
{
    public IReadOnlyList<T> Rows { get; init; } = Array.Empty<T>();
    public int TotalFiltrados { get; init; }
}

public sealed class CRegionRow
{
    public int Id { get; init; }
    public string Region { get; init; } = string.Empty;
    public DateTime FechaCreacion { get; init; }
    public DateTime FechaUltimaModificacion { get; init; }
}

public sealed class CRegionCreateRequest
{
    public string Region { get; set; } = string.Empty;
}

public sealed class CRegionUpdateRequest
{
    public int Id { get; set; }
    public string Region { get; set; } = string.Empty;
}

public sealed class CRegionDeleteRequest
{
    public int Id { get; set; }
}

public sealed class CRutaRow
{
    public int Id { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string Ruta { get; init; } = string.Empty;
    public string NombreArchivo { get; init; } = string.Empty;
    public DateTime FechaCreacion { get; init; }
    public DateTime FechaUltimaModificacion { get; init; }
}

public sealed class CRutaCreateRequest
{
    public string Descripcion { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
}

public sealed class CRutaUpdateRequest
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
}

public sealed class CRutaDeleteRequest
{
    public int Id { get; set; }
}

public sealed class CAlertaRow
{
    public int Id { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public int Severidad { get; init; }
    public bool ActivarAlerta { get; init; }
    public DateTime FechaCreacion { get; init; }
    public DateTime FechaUltimaModificacion { get; init; }
}

public sealed class CAlertaCreateRequest
{
    public string Descripcion { get; set; } = string.Empty;
    public int Severidad { get; set; }
    public bool ActivarAlerta { get; set; }
}

public sealed class CAlertaUpdateRequest
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int Severidad { get; set; }
    public bool ActivarAlerta { get; set; }
}

public sealed class CAlertaDeleteRequest
{
    public int Id { get; set; }
}

public sealed class CStatusTransaccionRow
{
    public int Id { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public DateTime FechaCreacion { get; init; }
    public DateTime FechaUltimaModificacion { get; init; }
}

public sealed class CStatusTransaccionCreateRequest
{
    public string Descripcion { get; set; } = string.Empty;
}

public sealed class CStatusTransaccionUpdateRequest
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public sealed class CStatusTransaccionDeleteRequest
{
    public int Id { get; set; }
}

public sealed class CDetalleTransaccionRow
{
    public int Id { get; init; }
    public string NombreDetalle { get; init; } = string.Empty;
    public int TipoDato { get; init; }
    public DateTime FechaCreacion { get; init; }
    public DateTime FechaUltimaModificacion { get; init; }
}

public sealed class CDetalleTransaccionCreateRequest
{
    public string NombreDetalle { get; set; } = string.Empty;
    public int TipoDato { get; set; }
}

public sealed class CDetalleTransaccionUpdateRequest
{
    public int Id { get; set; }
    public string NombreDetalle { get; set; } = string.Empty;
    public int TipoDato { get; set; }
}

public sealed class CDetalleTransaccionDeleteRequest
{
    public int Id { get; set; }
}

public sealed class CAtributoTransaccionRow
{
    public int Id { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public int TipoDato { get; init; }
    public DateTime FechaCreacion { get; init; }
    public DateTime FechaUltimaModificacion { get; init; }
}

public sealed class CAtributoTransaccionCreateRequest
{
    public string Descripcion { get; set; } = string.Empty;
    public int TipoDato { get; set; }
}

public sealed class CAtributoTransaccionUpdateRequest
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int TipoDato { get; set; }
}

public sealed class CAtributoTransaccionDeleteRequest
{
    public int Id { get; set; }
}

public sealed class CDispositivoEliminadoRow
{
    public int Id { get; init; }
    public string NombreDispositivo { get; init; } = string.Empty;
    public DateTime FechaCreacion { get; init; }
    public DateTime FechaUltimaModificacion { get; init; }
}

public sealed class CDispositivoEliminadoCreateRequest
{
    public string NombreDispositivo { get; set; } = string.Empty;
}

public sealed class CDispositivoEliminadoUpdateRequest
{
    public int Id { get; set; }
    public string NombreDispositivo { get; set; } = string.Empty;
}

public sealed class CDispositivoEliminadoDeleteRequest
{
    public int Id { get; set; }
}

public sealed class CatalogLocationRow
{
    public int Id { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public int IdRegion { get; init; }
    public string RegionNombre { get; init; } = string.Empty;
    public string? Ubicacion { get; init; }
    public string? Estado { get; init; }
}

public sealed class CatalogLocationCreateRequest
{
    public string LocationName { get; set; } = string.Empty;
    public int IdRegion { get; set; }
    public string? Ubicacion { get; set; }
    public string? Estado { get; set; }
}

public sealed class CatalogLocationUpdateRequest
{
    public int Id { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int IdRegion { get; set; }
    public string? Ubicacion { get; set; }
    public string? Estado { get; set; }
}

public sealed class CatalogLocationDeleteRequest
{
    public int Id { get; set; }
}

public sealed class CEstadoComboRow
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? HcKey { get; init; }
}
