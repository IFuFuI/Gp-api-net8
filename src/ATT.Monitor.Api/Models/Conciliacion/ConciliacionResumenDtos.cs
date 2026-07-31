namespace ATT.Monitor.Api.Models.Conciliacion;

/// <summary>Solicitud para agregado transaccional (sin SP nuevo; reutiliza <c>SP_GET_TRANSACCIONES</c>).</summary>
public sealed class ConciliacionResumenTransaccionalRequest
{
    public int Ignorar { get; set; }
    public int CantidadFila { get; set; } = 4000;
    public string? Filtro { get; set; }
    public string? Orden { get; set; } = "FECHA_REGISTRO";
    public string? Dir { get; set; } = "DESC";

    /// <summary>Inclusive, formato fecha local (yyyy-MM-dd recomendado).</summary>
    public string? FechaInicio { get; set; }

    /// <summary>Inclusive, formato fecha local.</summary>
    public string? FechaFin { get; set; }
}

public sealed class ConciliacionEstatusBucketDto
{
    public string Estatus { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

/// <summary>Respuesta de <c>POST api/Conciliacion/resumen-transaccional</c>.</summary>
public sealed class ConciliacionResumenTransaccionalResponse
{
    public int FilasTraidasDeSp { get; set; }
    public int MovimientosEnRango { get; set; }
    public decimal SumaMontos { get; set; }
    public int PosiblesAnomaliasEstatus { get; set; }
    public IReadOnlyList<ConciliacionEstatusBucketDto> PorEstatus { get; set; } = [];
    public IReadOnlyList<TransaccionOnlineRowApiDto> FilasEnRango { get; set; } = [];
}

public class CargaConciliacionDto
{
    public int Total { get; set; }
    public long ID_CARGA { get; set; }
    public string NOMBRE_ARCHIVO { get; set; }
    public string HASH_ARCHIVO { get; set; }
    public string USUARIO_CARGA { get; set; }
    public DateTime FECHA_CARGA { get; set; }
    public string ESTATUS_CARGA { get; set; }
    public bool ES_REPROCESO { get; set; }
    public long? ID_CARGA_ORIGEN { get; set; }
    public int TOTAL_REGISTROS { get; set; }
    public int TOTAL_CONCILIADOS { get; set; }
    public int TOTAL_SOLO_AUTOPAGO { get; set; }
    public int TOTAL_CANCELADOS { get; set; }
    public int TOTAL_NO_PROCESABLES { get; set; }
    public int TOTAL_DUPLICADOS_LLAVE { get; set; }
    public string OBSERVACION { get; set; }
}

public class IdCargaConciliacionResponse
{
    public int IdCarga { get; set; }
}
public class AutopagoDetalle
{
    public long ID_AUTOPAGO { get; set; }
    public long ID_CARGA { get; set; }
    public int NUM_LINEA { get; set; }

    public string REGION { get; set; }
    public string POS_ID { get; set; }
    public string TIENDA { get; set; }
    public string TIPO { get; set; }

    public string ORDEN_CRM_OMS { get; set; }
    public string TIPO_DOCUMENTO { get; set; }
    public string NUMERO_DOCUMENTO { get; set; }

    public string CONCEPTO_PAGO { get; set; }
    public string FORMA_PAGO { get; set; }

    public string? NOMBRE_CLIENTE { get; set; }

    public string TELEFONO_CUENTA { get; set; }
    public string CUENTA_CLIENTE { get; set; }

    public string CAJERO { get; set; }
    public string IDENTIFICADOR_CORTE { get; set; }

    public string? POLIZA_GL { get; set; }

    public string ESTATUS_CORTE { get; set; }
    public string FECHA_ENVIO_POLIZA { get; set; }

    public string TICKET { get; set; }
    public string CANCELADO { get; set; }

    public DateTime FECHA_TRANSACCION { get; set; }
    public TimeSpan HORA_TRANSACCION { get; set; }

    public decimal IMPORTE { get; set; }

    public string FECHA_TRANSACCION_ORIGEN { get; set; }
    public string HORA_TRANSACCION_ORIGEN { get; set; }
    public string IMPORTE_ORIGEN { get; set; }

    public string CAJERO_NORM { get; set; }
    public string CUENTA_CLIENTE_NORM { get; set; }

    public decimal IMPORTE_NORM { get; set; }

    public DateTime FECHA_OPERACION { get; set; }
    public DateTime FECHA_OPERACION_MINUTO { get; set; }

    public int? ID_TRANSACCION { get; set; }

    public string ESTATUS_CONCILIACION { get; set; }
    public string OBSERVACION_CONCILIACION { get; set; }

    public DateTime FECHA_CONCILIACION { get; set; }

    public bool DUPLICADO_EN_LOTE { get; set; }
}
