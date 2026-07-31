using System.Data;
using System.Globalization;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Conciliacion;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ATT.Monitor.Api.Infrastructure.Persistence;

public class ConciliacionDataService(IConfiguration config) : IConciliacion
{
     private string ConnectionString =>
        config.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:SqlServer.");

    public async Task<string> GetDetalleConciliacionAsync(IdCargaConciliacionResponse post,
             CancellationToken cancellationToken = default)
{
    using var connection = new SqlConnection(ConnectionString);

    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
    var result = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT dbo.FN_OBTENER_JSON_CARGA(@IdCarga)",
                new { IdCarga = post.IdCarga }).ConfigureAwait(false);

    return result ?? "[]";
}
    public async Task<string> GetConciliacionAsync(ConciliacionResumenTransaccionalRequest post,
             CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@IGNORAR", post.Ignorar, DbType.Int32);
        parameters.Add("@CANTIDAD_FILA", post.CantidadFila, DbType.Int32);
        parameters.Add("@FILTRO", post.Filtro, DbType.String);
        parameters.Add("@ORDEN", post.Orden, DbType.String);
        parameters.Add("@DIR", post.Dir, DbType.String);
        parameters.Add("@FechaInicio", post.FechaInicio, DbType.String);
        parameters.Add("@FechaFin", post.FechaFin, DbType.String);

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QueryFirstOrDefaultAsync<ConciliacionCargaJsonRow>(
                    "SP_GET_CONCILIACION_CARGA",
                    parameters,
                    commandType: CommandType.StoredProcedure).ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(row?.JsonResult) ? "[]" : row.JsonResult;
    }

    public async Task<(int IdCarga, bool EsReproceso, int? IdCargaOrigen)> RegistrarCargaAsync(
        string nombreArchivo,
        string hashArchivo,
        string usuarioCarga,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var parameters = new DynamicParameters();
        parameters.Add("@NOMBRE_ARCHIVO",  nombreArchivo,  DbType.String);
        parameters.Add("@HASH_ARCHIVO",    hashArchivo,    DbType.String);
        parameters.Add("@USUARIO_CARGA",   usuarioCarga,   DbType.String);
        parameters.Add("@ID_CARGA",        dbType: DbType.Int32,  direction: ParameterDirection.Output);
        parameters.Add("@ES_REPROCESO",    dbType: DbType.Boolean, direction: ParameterDirection.Output);
        parameters.Add("@ID_CARGA_ORIGEN", dbType: DbType.Int32,  direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "dbo.SP_AUTOPAGO_REGISTRAR_CARGA",
            parameters,
            commandType: CommandType.StoredProcedure).ConfigureAwait(false);

        var idCarga       = parameters.Get<int>("@ID_CARGA");
        var esReproceso   = parameters.Get<bool>("@ES_REPROCESO");
        var idCargaOrigen = parameters.Get<int?>("@ID_CARGA_ORIGEN");

        return (idCarga, esReproceso, idCargaOrigen);
    }

    public async Task InsertarDetalleAsync(
        int idCarga,
        IEnumerable<AutopagoDetalleRow> detalle,
        CancellationToken cancellationToken = default)
    {
        var tvp = BuildTvp(detalle);

        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var parameters = new DynamicParameters();
        parameters.Add("@ID_CARGA",             idCarga,  DbType.Int32);
        parameters.Add("@DETALLE",              tvp.AsTableValuedParameter("dbo.TVP_AUTOPAGO_DETALLE"));
        parameters.Add("@EJECUTAR_CONCILIACION", 1,        DbType.Int32);
        parameters.Add("@REEMPLAZAR_DETALLE",    0,        DbType.Int32);

        await connection.ExecuteAsync(
            "dbo.SP_AUTOPAGO_INSERTAR_DETALLE",
            parameters,
            commandType: CommandType.StoredProcedure).ConfigureAwait(false);
    }

    // ─── Builder del TVP ────────────────────────────────────────────────────

    private static DataTable BuildTvp(IEnumerable<AutopagoDetalleRow> rows)
    {
        var dt = new DataTable();

        dt.Columns.Add("NUM_LINEA",               typeof(int));
        dt.Columns.Add("REGION",                  typeof(string));
        dt.Columns.Add("POS_ID",                  typeof(string));
        dt.Columns.Add("TIENDA",                  typeof(string));
        dt.Columns.Add("TIPO",                    typeof(string));
        dt.Columns.Add("ORDEN_CRM_OMS",           typeof(string));
        dt.Columns.Add("TIPO_DOCUMENTO",          typeof(string));
        dt.Columns.Add("NUMERO_DOCUMENTO",        typeof(string));
        dt.Columns.Add("CONCEPTO_PAGO",           typeof(string));
        dt.Columns.Add("FORMA_PAGO",              typeof(string));
        dt.Columns.Add("NOMBRE_CLIENTE",          typeof(string));
        dt.Columns.Add("TELEFONO_CUENTA",         typeof(string));
        dt.Columns.Add("CUENTA_CLIENTE",          typeof(string));
        dt.Columns.Add("CAJERO",                  typeof(string));
        dt.Columns.Add("IDENTIFICADOR_CORTE",     typeof(string));
        dt.Columns.Add("POLIZA_GL",               typeof(string));
        dt.Columns.Add("ESTATUS_CORTE",           typeof(string));
        dt.Columns.Add("FECHA_ENVIO_POLIZA",      typeof(string));
        dt.Columns.Add("TICKET",                  typeof(string));
        dt.Columns.Add("CANCELADO",               typeof(string));
        dt.Columns.Add("FECHA_TRANSACCION",       typeof(DateTime));
        dt.Columns.Add("HORA_TRANSACCION",        typeof(TimeSpan));
        dt.Columns.Add("IMPORTE",                 typeof(decimal));
        dt.Columns.Add("FECHA_TRANSACCION_ORIGEN",typeof(string));
        dt.Columns.Add("HORA_TRANSACCION_ORIGEN", typeof(string));
        dt.Columns.Add("IMPORTE_ORIGEN",          typeof(string));

        int numLinea = 1;
        foreach (var r in rows)
        {
            // Parsear fecha y hora para los tipos correctos del TVP
            DateTime? fecha = ParseFecha(r.FechaTransaccion);
            TimeSpan? hora  = ParseHora(r.HoraTransaccion);

            dt.Rows.Add(
                numLinea++,
                r.Region             ?? (object)DBNull.Value,
                r.PosId              ?? (object)DBNull.Value,
                r.Tienda             ?? (object)DBNull.Value,
                r.Tipo               ?? (object)DBNull.Value,
                r.OrdenCrmOms        ?? (object)DBNull.Value,
                r.TipoDocumento      ?? (object)DBNull.Value,
                r.NumeroDocumento    ?? (object)DBNull.Value,
                r.ConceptoPago       ?? (object)DBNull.Value,
                r.FormaPago          ?? (object)DBNull.Value,
                r.NombreCliente      ?? (object)DBNull.Value,
                r.TelefonoCuenta     ?? (object)DBNull.Value,
                r.CuentaCliente      ?? (object)DBNull.Value,
                r.Cajero             ?? (object)DBNull.Value,
                r.IdentificadorCorte ?? (object)DBNull.Value,
                r.PolizaGl           ?? (object)DBNull.Value,
                r.EstatusCorte       ?? (object)DBNull.Value,
                r.FechaEnvioPoliza   ?? (object)DBNull.Value,
                r.Ticket             ?? (object)DBNull.Value,
                r.Cancelado          ?? (object)DBNull.Value,
                fecha.HasValue       ? fecha.Value : (object)DBNull.Value,
                hora.HasValue        ? hora.Value  : (object)DBNull.Value,
                r.Importe.HasValue   ? r.Importe.Value : (object)DBNull.Value,
                r.FechaTransaccion   ?? (object)DBNull.Value,   // FECHA_TRANSACCION_ORIGEN (raw)
                r.HoraTransaccion    ?? (object)DBNull.Value,   // HORA_TRANSACCION_ORIGEN  (raw)
                r.Importe.HasValue   ? r.Importe.Value.ToString(CultureInfo.InvariantCulture)
                                     : (object)DBNull.Value     // IMPORTE_ORIGEN (raw string)
            );
        }

        return dt;
    }

    private static DateTime? ParseFecha(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        string[] formatos = ["yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy"];
        if (DateTime.TryParseExact(raw.Trim(), formatos,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        return null;
    }

    private static TimeSpan? ParseHora(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        if (TimeSpan.TryParse(raw.Trim(), out var ts))
            return ts;

        return null;
    }

    private sealed class ConciliacionCargaJsonRow
    {
        public string? JsonResult { get; set; }
    }
}