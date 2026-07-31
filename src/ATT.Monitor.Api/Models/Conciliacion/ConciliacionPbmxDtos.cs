namespace ATT.Monitor.Api.Models.Conciliacion;

/// <summary>Contrato alineado a vista MVC PBMX y respuesta enriquecida futura de <c>POST cargar-archivo</c>.</summary>
public sealed class ConciliacionPbmxProcesarResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int IdCarga { get; set; }
    public bool EsReproceso { get; set; }
    public int? IdCargaOrigen { get; set; }
    public int RegistrosProcesados { get; set; }
    public int RegistrosCorrectos { get; set; }
    public int RegistrosConDiferencias { get; set; }
    public decimal TotalConciliado { get; set; }
    public int FilasLeidas { get; set; }
    public int FilasConErrorParseo { get; set; }
    public IReadOnlyList<ConciliacionPbmxDiferenciaDto> Diferencias { get; set; } = [];
    public IReadOnlyList<string> ErroresParseo { get; set; } = [];
}

public sealed class ConciliacionPbmxDiferenciaDto
{
    public string Transaccion { get; set; } = string.Empty;
    public string Diferencia { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}

/// <summary>Ítem de historial (<c>SP_GET_CONCILIACION_CARGA</c> / mock MVC).</summary>
public sealed class ConciliacionPbmxHistorialItemDto
{
    public long IdCarga { get; set; }
    public string Fecha { get; set; } = string.Empty;
    public string Hora { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Archivo { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty;
    public int Registros { get; set; }
    public int RegistrosConciliados { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
}

public sealed class ConciliacionPbmxHistorialRequest
{
    public string? Tipo { get; set; }
    public int Ignorar { get; set; }
    public int CantidadFila { get; set; } = 25;
    public string? Filtro { get; set; }
    public string? FechaInicio { get; set; }
    public string? FechaFin { get; set; }
}

public sealed class ConciliacionPbmxHistorialResponse
{
    public int FilasTraidasDeSp { get; set; }
    public int Total { get; set; }
    public IReadOnlyList<ConciliacionPbmxHistorialItemDto> Items { get; set; } = [];
}
