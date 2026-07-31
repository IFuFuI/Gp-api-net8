using System.Data;
using System.Globalization;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Configuration;
using ATT.Monitor.Api.Infrastructure.Resilience;
using ATT.Monitor.Api.Models.Dashboard;
using ATT.Monitor.Api.Models.Login;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace ATT.Monitor.Api.Infrastructure.Persistence;

/// <summary>Paridad <c>DashboardData</c> API v1 (Dapper).</summary>
public sealed class DashboardDataService(
    IConfiguration configuration,
    ILogger<DashboardDataService> logger,
    IOptionsMonitor<MonitorAgentOptions> monitorAgent) : IDashboardData
{
    private string ConnectionString =>
        configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:SqlServer.");

    private SqlConnection CreateConnection() => new(ConnectionString);

    public async Task<MCards?> GetCardsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<MCards>(
            new CommandDefinition(
                "dbo.SP_GET_CARDS_DASBOARD",
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    public async Task<IEnumerable<FallaResponse>> GetFallasAsync(FallaRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        const string sql = """
            SELECT *
            FROM dbo.FN_CONSULTA_FALLAS(
                @PAGE_NUMBER,
                @PAGE_SIZE,
                @BUSCAR,
                @ORDER_BY,
                @ORDER_DIR,
                @ID_DISPO,
                @FECHA_INI,
                @FECHA_FIN
            );
            """;
        var param = new
        {
            PAGE_NUMBER = request.PageNumber,
            PAGE_SIZE = request.PageSize,
            BUSCAR = string.IsNullOrWhiteSpace(request.Buscar) ? null : request.Buscar.Trim(),
            ORDER_BY = request.OrderBy,
            ORDER_DIR = request.OrderDir,
            ID_DISPO = request.ID_DISPO == 0 ? (int?)null : request.ID_DISPO,
            FECHA_INI = ParseFallaFilterDate(request.FECHA_INI),
            FECHA_FIN = ParseFallaFilterDate(request.Fecha_FIN)
        };
        return await connection.QueryAsync<FallaResponse>(
            new CommandDefinition(sql, param, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<ProcedureResultDto?> InsertKeepAliveAsync(KeepAliveRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        var p = new DynamicParameters();
        p.Add("@KEEP_ALIVE", request.KeepAlive);
        p.Add("@STATUS", request.Status);
        p.Add("@ID_ATM", request.IdCajero);
        return await connection.QueryFirstOrDefaultAsync<ProcedureResultDto>(
            new CommandDefinition(
                "dbo.SP_KEEP_ALIVE",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    public async Task<ProcedureResultDto?> InsertAtmAsync(InsertAtmRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        var p = new DynamicParameters();
        p.Add("@NUMEROCAJERO", request.NumeroCajero);
        p.Add("@SUCURSAL", request.Sucursal);
        p.Add("@NOMBRE", request.Nombre);
        p.Add("@DIRECCION", request.Direccion);
        p.Add("@ESTADO", request.Estado);
        p.Add("@ADMINISTRADOPOR", request.AdministradoPor);
        p.Add("@ZONA", request.Zona);
        p.Add("@MODELO", request.Modelo);
        p.Add("@UBICACION", request.Ubicacion);
        p.Add("@SERIE", request.Serie);
        p.Add("@CANAL", request.Canal);
        p.Add("@TIPO", request.Tipo);
        p.Add("@FECHAHORA", request.FechaHora);
        p.Add("@OBSERVACIONES", request.Observaciones);
        return await connection.QueryFirstOrDefaultAsync<ProcedureResultDto>(
            new CommandDefinition(
                "dbo.SP_INSERTARCAJERO",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    public async Task<ProcedureResultDto?> InsertAtmContadoresAsync(InsertAtmContadoresRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ID_CAJERO", request.IdCajero);
        p.Add("@INI_C1", request.IniC1);
        p.Add("@INI_C2", request.IniC2);
        p.Add("@INI_C3", request.IniC3);
        p.Add("@INI_C4", request.IniC4);
        p.Add("@INI_C5", request.IniC5);
        p.Add("@INI_C6", request.IniC6);
        p.Add("@REM_C1", request.RemC1);
        p.Add("@REM_C2", request.RemC2);
        p.Add("@REM_C3", request.RemC3);
        p.Add("@REM_C4", request.RemC4);
        p.Add("@REM_C5", request.RemC5);
        p.Add("@REM_C6", request.RemC6);
        p.Add("@DISP_C1", request.DispC1);
        p.Add("@DISP_C2", request.DispC2);
        p.Add("@DISP_C3", request.DispC3);
        p.Add("@DISP_C4", request.DispC4);
        p.Add("@DISP_C5", request.DispC5);
        p.Add("@DISP_C6", request.DispC6);
        p.Add("@RECH_C1", request.RechC1);
        p.Add("@RECH_C2", request.RechC2);
        p.Add("@RECH_C3", request.RechC3);
        p.Add("@RECH_C4", request.RechC4);
        p.Add("@RECH_C5", request.RechC5);
        p.Add("@RECH_C6", request.RechC6);
        p.Add("@CDOM_C1", request.CDOM_C1);
        p.Add("@CDOM_C2", request.CDOM_C2);
        p.Add("@CDOM_C3", request.CDOM_C3);
        p.Add("@CDOM_C4", request.CDOM_C4);
        p.Add("@CDOM_C5", request.CDOM_C5);
        p.Add("@CDOM_C6", request.CDOM_C6);
        return await connection.QueryFirstOrDefaultAsync<ProcedureResultDto>(
            new CommandDefinition(
                "dbo.SP_INSERTAR_CONTADORES_CDM",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    public async Task<string?> ComandoAtmAsync(string idCajero, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ID_CAJERO", idCajero);
        var raw = await connection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(
                "dbo.SP_GET_COMANDO_ATM",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);

        return string.IsNullOrEmpty(raw) ? raw : AtmComandoPathCodec.NormalizeGetComandoJson(raw);
    }

    public async Task<string?> InsertDispositivoAsync(InsertDispositivoRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ID_CAJERO", request.IdCajero);
        p.Add("@NOMBRE_DISPOSITIVO", request.NombreDispositivo);
        p.Add("@STATUS", request.Status);
        return await connection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(
                "dbo.SP_InsertarStatusDispositivo",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    public async Task<string?> InsertDispositivoAsyncTipo(InsertDispositivoRequestM request, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        string? result = null;
        foreach (var item in request.Data)
        {
            var p = new DynamicParameters();
            p.Add("@ID_CAJERO", request.IdCajero);
            p.Add("@NOMBRE_DISPOSITIVO", request.NombreDispositivo);
            p.Add("@STATUS", item.Status);
            p.Add("@TIPO", item.Tipo);
            result = await connection.QueryFirstOrDefaultAsync<string>(
                new CommandDefinition(
                    "dbo.SP_InsertarStatusDispositivo_tipo",
                    p,
                    cancellationToken: cancellationToken,
                    commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        }

        return result;
    }

    public async Task<string?> InsertTransaccionAsync(TransaccionRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        var p = new DynamicParameters();
        p.Add("@EP", request.EP);
        p.Add("@FECHA", request.FECHA);
        p.Add("@TIPO", request.TIPO);
        p.Add("@ESTATUS", request.ESTATUS);
        p.Add("@TIPO_PAGO", request.TipOPAGO);
        p.Add("@CLIENTE", request.CLIENTE);
        p.Add("@NOMBRE_CLIENTE", request.NombrECLIENTE);
        p.Add("@MONTO", request.MONTO ?? 0);
        p.Add("@CAMBIO", request.CAMBIO ?? 0);
        p.Add("@FOLIO", request.FOLIO);
        p.Add("@CAMBIO_INCOMPLETO", request.CAMBIO_INCOMPLETO);
        p.Add("@CODIGO_ERROR", request.CodigOERROR);
        p.Add("@MOTIVO_RECHAZO", request.MOTIVO_RECHAZO);
        p.Add("@DN", request.DN);
        p.Add("@CODIGO_AUTORIZACION", request.CODIGO_AUTORIZACION);
        p.Add("@NO_TARJETA", request.NO_TARJETA);
        p.Add("@REFERENCIA", request.REFERENCIA);
        p.Add("@REFERENCIA_EP", request.REFERENCIA_EP);
        p.Add("@COLOR", request.COLOR);
        p.Add("@JOURNAL", request.JOURNAL);
        p.Add("@FECHA_REGISTRO", request.FECHA_REGISTRO);
        p.Add("@OPERACION", request.Operacion);
        return await connection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(
                "dbo.SP_INSERT_TRANSACCION",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    public async Task<string?> GetTransaccionesAsync(PostTransaccion request, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Ignorar", request.Ignorar);
        p.Add("@Cantidad_Fila", request.Cantidad_Fila);
        p.Add("@Filtro", request.Filtro);
        p.Add("@Orden", request.Orden);
        p.Add("@Dir", request.Dir);
        return await connection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(
                "dbo.SP_GET_TRANSACCIONES",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    public async Task<IEnumerable<FallaEPResponse>> GetTotalEPAsync(FallaEPRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        var parametros = new DynamicParameters();
        parametros.Add("@PAGE_NUMBER", request.PageNumber);
        parametros.Add("@PAGE_SIZE", request.PageSize);
        parametros.Add("@BUSCAR", request.Buscar);
        parametros.Add("@ORDER_BY", request.OrderBy);
        parametros.Add("@ORDER_DIR", request.OrderDir);
        const string sql = """
            SELECT * FROM dbo.FN_TOTAL_EP_PAGINADO(@PAGE_NUMBER,@PAGE_SIZE,@BUSCAR,@ORDER_BY,@ORDER_DIR)
            """;
        return await connection.QueryAsync<FallaEPResponse>(
            new CommandDefinition(sql, parametros, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<DashboardResponseEP> GetEpDashboardMonitorAsync(CancellationToken cancellationToken = default)
    {
        return await SqlTransientRetry.ExecuteAsync(async () =>
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var mapa = (await connection.QueryAsync<UbicacionResponse>(
                new CommandDefinition(
                    "dbo.SP_EP_MONITOR_UBICACION",
                    cancellationToken: cancellationToken,
                    commandType: CommandType.StoredProcedure)).ConfigureAwait(false)).ToList();

            IReadOnlyList<ResponseDetalleMonitor> estadisticasRows;
            try
            {
                estadisticasRows = (await connection.QueryAsync<ResponseDetalleMonitor>(
                    new CommandDefinition(
                        "dbo.SP_EP_ESTADISTICAS",
                        cancellationToken: cancellationToken,
                        commandType: CommandType.StoredProcedure)).ConfigureAwait(false)).ToList();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SP_EP_ESTADISTICAS no devolvió datos válidos; se derivarán estadísticas del mapa.");
                estadisticasRows = [];
            }

            var estadisticas = ResolveMonitorEstadisticas(estadisticasRows, mapa);
            if (estadisticasRows.Count == 0 && mapa.Count > 0)
            {
                logger.LogInformation(
                    "Estadísticas del monitor derivadas del mapa ({Ubicaciones} ubicaciones).",
                    mapa.Count);
            }

            return new DashboardResponseEP
            {
                Estadisticas = estadisticas,
                Mapa = mapa
            };
        }).ConfigureAwait(false);
    }

    private static ResponseDetalleMonitor ResolveMonitorEstadisticas(
        IReadOnlyList<ResponseDetalleMonitor> spRows,
        IReadOnlyList<UbicacionResponse> mapa)
    {
        if (spRows.Count == 1)
            return NormalizeMonitorStats(spRows[0]);

        if (spRows.Count > 1)
        {
            return new ResponseDetalleMonitor
            {
                EP_Monitoreados = spRows.Count,
                EP_Activos = spRows.Sum(static r => r.EP_Activos),
                EP_Con_Fallas = spRows.Sum(static r => r.EP_Con_Fallas),
                ULTIMA_ACTUALIZACION = PickUltimaActualizacion(spRows[0].ULTIMA_ACTUALIZACION)
            };
        }

        if (mapa.Count > 0)
        {
            return new ResponseDetalleMonitor
            {
                EP_Monitoreados = mapa.Count(static m => m.EP_Total > 0),
                EP_Activos = mapa.Sum(static m => Math.Max(0, m.EP_Total - m.EP_Con_Falla)),
                EP_Con_Fallas = mapa.Sum(static m => m.EP_Con_Falla),
                ULTIMA_ACTUALIZACION = PickUltimaActualizacion(null)
            };
        }

        return CreateEmptyMonitorStats();
    }

    private static ResponseDetalleMonitor NormalizeMonitorStats(ResponseDetalleMonitor stats)
    {
        stats.ULTIMA_ACTUALIZACION = PickUltimaActualizacion(stats.ULTIMA_ACTUALIZACION);
        return stats;
    }

    private static string PickUltimaActualizacion(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)
            : value.Trim();

    private static ResponseDetalleMonitor CreateEmptyMonitorStats() => new()
    {
        EP_Monitoreados = 0,
        EP_Activos = 0,
        EP_Con_Fallas = 0,
        ULTIMA_ACTUALIZACION = PickUltimaActualizacion(null)
    };

    public async Task<IEnumerable<EPDatosResponse>> GetTotalEPDatosAsync(EPRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        const string sql = "SELECT * FROM dbo.FN_EP_DATOS(@ID_EP)";
        return await connection.QueryAsync<EPDatosResponse>(
            new CommandDefinition(sql, new { ID_EP = request.EP }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IEnumerable<EPContadoresResponse>> GetTotalEPContadoresAsync(EPRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        const string sql = "SELECT * FROM dbo.FN_CONTADORES_EP(@ID_EP)";
        var data = await connection.QueryFirstOrDefaultAsync<dynamic>(
            new CommandDefinition(sql, new { ID_EP = request.EP }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (data is null)
            return Enumerable.Empty<EPContadoresResponse>();

        static int ReadInt(dynamic row, string name)
        {
            try
            {
                if (row is not IDictionary<string, object> dict)
                    return 0;
                var match = dict.Keys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
                if (match is null || !dict.TryGetValue(match, out var v) || v is null or DBNull)
                    return 0;
                return Convert.ToInt32(v, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        var result = new EPContadoresResponse
        {
            TOTAL_REMANENTE = ReadInt(data, "TOTAL_REMANENTE"),
            Iniciales = new ContadoresIniciales
            {
                INI_C1 = ReadInt(data, "INI_C1"),
                INI_C2 = ReadInt(data, "INI_C2"),
                INI_C3 = ReadInt(data, "INI_C3"),
                INI_C4 = ReadInt(data, "INI_C4")
            },
            Remanente = new ContadoresRemanente
            {
                REM_C1 = ReadInt(data, "REM_C1"),
                REM_C2 = ReadInt(data, "REM_C2"),
                REM_C3 = ReadInt(data, "REM_C3"),
                REM_C4 = ReadInt(data, "REM_C4")
            },
            Dispensados = new ContadoresDispensados
            {
                DISP_C1 = ReadInt(data, "DISP_C1"),
                DISP_C2 = ReadInt(data, "DISP_C2"),
                DISP_C3 = ReadInt(data, "DISP_C3"),
                DISP_C4 = ReadInt(data, "DISP_C4")
            },
            Rechazos = new ContadoresRechazos
            {
                RECH_C1 = ReadInt(data, "RECH_C1"),
                RECH_C2 = ReadInt(data, "RECH_C2"),
                RECH_C3 = ReadInt(data, "RECH_C3"),
                RECH_C4 = ReadInt(data, "RECH_C4")
            }
        };

        return [result];
    }

    public async Task<IEnumerable<EstatusEquipoEPResponse>> GetEstatusEEPAsync(EPRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        var parametros = new DynamicParameters();
        parametros.Add("@ID_EP", request.EP);
        const string sql = "SELECT * FROM dbo.FN_ESTADO_ACTUAL_EP(@ID_EP)";
        return await connection.QueryAsync<EstatusEquipoEPResponse>(
            new CommandDefinition(sql, parametros, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<AgenteComunicacionEpResponse?> GetAgenteComunicacionEpAsync(
        EPRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.EP))
            return null;

        var ep = request.EP.Trim();
        var threshold = Math.Clamp(monitorAgent.CurrentValue.KeepAliveOnlineThresholdMinutes, 1, 120);

        AgenteComunicacionDbRow? row;
        try
        {
            await using var connection = CreateConnection();
            row = await connection.QueryFirstOrDefaultAsync<AgenteComunicacionDbRow>(
                new CommandDefinition(
                    "SELECT * FROM dbo.FN_GET_AGENTE_COMUNICACION_EP(@ID_EP)",
                    new { ID_EP = ep },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (SqlException ex) when (IsMissingFunction(ex, "FN_GET_AGENTE_COMUNICACION_EP"))
        {
            await using var connection = CreateConnection();
            row = await connection.QueryFirstOrDefaultAsync<AgenteComunicacionDbRow>(
                new CommandDefinition(
                    """
                    SELECT RTRIM(K.ID_ATM) AS ID_ATM, K.KEEP_ALIVE, K.[STATUS], K.FECHA_ULTIMA_MODIFICACION
                    FROM dbo.BD_ATMS_STATUS AS K
                    WHERE RTRIM(K.ID_ATM) = @ID_EP
                    """,
                    new { ID_EP = ep },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        return AgenteComunicacionEvaluator.Evaluate(row, threshold);
    }

    public async Task<string?> GetTransaccionesEpAsync(
        PostTransaccionEp request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.EP))
            return "[]";

        var desde = ParseTransaccionEpDate(request.FechaDesde) ?? DateTime.Today;
        var hasta = ParseTransaccionEpDate(request.FechaHasta) ?? desde;
        if (desde > hasta)
            (desde, hasta) = (hasta, desde);

        await using var connection = CreateConnection();
        var p = new DynamicParameters();
        p.Add("@EP", request.EP.Trim());
        p.Add("@FechaDesde", desde.Date);
        p.Add("@FechaHasta", hasta.Date);
        p.Add("@Ignorar", request.Ignorar);
        p.Add("@Cantidad_Fila", request.Cantidad_Fila <= 0 ? 25 : request.Cantidad_Fila);
        p.Add("@Orden", string.IsNullOrWhiteSpace(request.Orden) ? "FECHA" : request.Orden.Trim());
        p.Add("@Dir", string.IsNullOrWhiteSpace(request.Dir) ? "desc" : request.Dir.Trim());

        try
        {
            return await connection.QueryFirstOrDefaultAsync<string>(
                new CommandDefinition(
                    "dbo.SP_GET_TRANSACCIONES_EP",
                    p,
                    cancellationToken: cancellationToken,
                    commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        }
        catch (SqlException ex) when (IsMissingProcedure(ex, "SP_GET_TRANSACCIONES_EP"))
        {
            logger.LogWarning(ex, "SP_GET_TRANSACCIONES_EP no disponible; aplicar script 2026_05_27_detalle_ep_comunicacion_transacciones.");
            return "[]";
        }
    }

    private static DateTime? ParseTransaccionEpDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        if (DateTime.TryParse(raw.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var iso))
            return iso.Date;
        if (DateTime.TryParse(raw.Trim(), CultureInfo.GetCultureInfo("es-MX"), DateTimeStyles.AssumeLocal, out var mx))
            return mx.Date;
        return null;
    }

    public async Task<IEnumerable<CatalogoEquipoRow>> GetCatalogoEquiposAsync(
        CatalogoEquiposRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await QueryCatalogoEquiposAsync(request, useRegionFiltroParam: true, cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException ex) when (IsMissingProcedureParameter(ex, "@REGION_FILTRO"))
        {
            var legacy = AdaptCatalogoRequestForLegacyRegionFilter(request);
            return await QueryCatalogoEquiposAsync(legacy, useRegionFiltroParam: false, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<CatalogoEquiposResumenRegionResponse> GetCatalogoEquiposResumenRegionAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await QueryCatalogoEquiposResumenRegionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException ex) when (IsMissingProcedure(ex, "SP_GET_CatalogoEquiposResumenRegion"))
        {
            return await QueryCatalogoEquiposResumenRegionInlineAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IEnumerable<CatalogoEquipoRow>> QueryCatalogoEquiposAsync(
        CatalogoEquiposRequest request,
        bool useRegionFiltroParam,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        var p = new DynamicParameters();
        p.Add("@IGNORAR", request.Ignorar);
        p.Add("@CANTIDAD_FILA", request.CantidadFila);
        p.Add("@FILTRO", request.Filtro ?? string.Empty);
        if (useRegionFiltroParam)
        {
            p.Add("@REGION_FILTRO", string.IsNullOrWhiteSpace(request.RegionFiltro)
                ? null
                : request.RegionFiltro.Trim());
        }

        p.Add("@ORDEN", request.Orden);
        p.Add("@DIR", request.Dir);
        return await connection.QueryAsync<CatalogoEquipoRow>(
            new CommandDefinition(
                "dbo.SP_GET_CatalogoEquipos",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    private async Task<CatalogoEquiposResumenRegionResponse> QueryCatalogoEquiposResumenRegionAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        var rows = (await connection.QueryAsync<CatalogoEquiposRegionKpiAgg>(
            new CommandDefinition(
                "dbo.SP_GET_CatalogoEquiposResumenRegion",
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false)).AsList();

        return BuildResumenRegion(rows);
    }

    private async Task<CatalogoEquiposResumenRegionResponse> QueryCatalogoEquiposResumenRegionInlineAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        const string sql = """
            SELECT
                CASE
                    WHEN NULLIF(LTRIM(RTRIM(ISNULL(R.REGION, D.REGION))), '') IS NULL
                    THEN N'(Sin región)'
                    ELSE LTRIM(RTRIM(ISNULL(R.REGION, D.REGION)))
                END AS REGION,
                COUNT(*) AS CANTIDAD
            FROM dbo.Device_Configuration D
            LEFT JOIN dbo.Catalog_Locations loc ON D.Id_Location = loc.Id
            LEFT JOIN dbo.C_REGION R ON R.ID = loc.Id_Region
            WHERE D.IsActive = 1
            GROUP BY
                CASE
                    WHEN NULLIF(LTRIM(RTRIM(ISNULL(R.REGION, D.REGION))), '') IS NULL
                    THEN N'(Sin región)'
                    ELSE LTRIM(RTRIM(ISNULL(R.REGION, D.REGION)))
                END
            ORDER BY REGION
            """;
        var rows = (await connection.QueryAsync<CatalogoEquiposRegionKpiAgg>(
            new CommandDefinition(sql, cancellationToken: cancellationToken, commandType: CommandType.Text))
            .ConfigureAwait(false)).AsList();
        return BuildResumenRegion(rows);
    }

    private static CatalogoEquiposResumenRegionResponse BuildResumenRegion(
        IReadOnlyList<CatalogoEquiposRegionKpiAgg> rows)
    {
        var items = rows.Select(r => new CatalogoEquiposRegionKpiRow
        {
            Region = r.REGION,
            Cantidad = r.CANTIDAD
        }).ToList();
        return new CatalogoEquiposResumenRegionResponse
        {
            TotalCajeros = items.Sum(r => r.Cantidad),
            PorRegion = items
        };
    }

    private static CatalogoEquiposRequest AdaptCatalogoRequestForLegacyRegionFilter(CatalogoEquiposRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RegionFiltro))
            return request;

        if (!string.IsNullOrWhiteSpace(request.Filtro))
        {
            return new CatalogoEquiposRequest
            {
                Ignorar = request.Ignorar,
                CantidadFila = request.CantidadFila,
                Filtro = request.Filtro,
                Orden = request.Orden,
                Dir = request.Dir
            };
        }

        return new CatalogoEquiposRequest
        {
            Ignorar = request.Ignorar,
            CantidadFila = request.CantidadFila,
            Filtro = request.RegionFiltro.Trim(),
            Orden = request.Orden,
            Dir = request.Dir
        };
    }

    private static bool IsMissingProcedureParameter(SqlException ex, string parameterName)
    {
        var msg = ex.Message;
        return msg.Contains(parameterName, StringComparison.OrdinalIgnoreCase)
               && (msg.Contains("not a parameter", StringComparison.OrdinalIgnoreCase)
                   || msg.Contains("no es un par", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsMissingProcedure(SqlException ex, string procedureName)
        => ex.Message.Contains(procedureName, StringComparison.OrdinalIgnoreCase)
           && (ex.Message.Contains("Could not find stored procedure", StringComparison.OrdinalIgnoreCase)
               || ex.Message.Contains("no se encontr", StringComparison.OrdinalIgnoreCase)
               || ex.Number == 2812);

    private static bool IsMissingFunction(SqlException ex, string functionName)
        => ex.Message.Contains(functionName, StringComparison.OrdinalIgnoreCase)
           && (ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
               || ex.Message.Contains("nombre de objeto no válido", StringComparison.OrdinalIgnoreCase)
               || ex.Number is 208 or 4121);

    private sealed class CatalogoEquiposRegionKpiAgg
    {
        public string REGION { get; set; } = string.Empty;
        public int CANTIDAD { get; set; }
    }

    public async Task<EquipoDetalleMvcResponse?> GetEquipoDetalleMvcAsync(
        EquipoDetalleMvcRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
            return null;

        var id = request.Id.Trim();

        async Task<EquipoInfoMvcDto?> LoadInfoAsync()
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return await connection.QuerySingleOrDefaultAsync<EquipoInfoMvcDto>(
                new CommandDefinition(
                    "dbo.SP_GET_EquiposDetalle",
                    new { ID = id },
                    cancellationToken: cancellationToken,
                    commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        }

        async Task<EquipoSoMvcDto?> LoadSoAsync()
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return await connection.QuerySingleOrDefaultAsync<EquipoSoMvcDto>(
                new CommandDefinition(
                    "SELECT * FROM dbo.FN_GET_EQUIPOSDETALLE_SO (@ID)",
                    new { ID = id },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        async Task<EquipoDiscoMvcDto?> LoadDdAsync()
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return await connection.QuerySingleOrDefaultAsync<EquipoDiscoMvcDto>(
                new CommandDefinition(
                    "SELECT * FROM dbo.FN_GET_EQUIPOSDETALLE_DD (@ID)",
                    new { ID = id },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        async Task<EquipoEstatusSoftwareMvcDto?> LoadStatusAsync()
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return await connection.QuerySingleOrDefaultAsync<EquipoEstatusSoftwareMvcDto>(
                new CommandDefinition(
                    "SELECT * FROM dbo.FN_GET_EQUIPOSDETALLE_STATUS (@ID)",
                    new { ID = id },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        var infoTask = LoadInfoAsync();
        var soTask = LoadSoAsync();
        var ddTask = LoadDdAsync();
        var stTask = LoadStatusAsync();
        await Task.WhenAll(infoTask, soTask, ddTask, stTask).ConfigureAwait(false);

        return new EquipoDetalleMvcResponse
        {
            Informacion = await infoTask.ConfigureAwait(false),
            SistemaOperativo = await soTask.ConfigureAwait(false),
            DiscoDuro = await ddTask.ConfigureAwait(false),
            EstatusSoftware = await stTask.ConfigureAwait(false)
        };
    }

    public async Task<IEnumerable<CardsDash>> GetCardsDashAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        return await connection.QueryAsync<CardsDash>(
            new CommandDefinition(
                "dbo.SP_GET_DASHBOARD",
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Dictionary<string, string>>> GetRolloutReportAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync(
            new CommandDefinition(
                "dbo.SP_MONITORROLLOUTSELECT",
                new { ID = " " },
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);

        var list = new List<Dictionary<string, string>>();
        foreach (var row in rows)
        {
            if (row is not IDictionary<string, object> dict)
                continue;

            var flat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in dict)
                flat[kv.Key] = kv.Value?.ToString() ?? string.Empty;

            list.Add(flat);
        }

        return list;
    }

    public async Task<IReadOnlyList<ReporteCatalogRow>> GetReportesCatalogAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<ReporteCatalogRow>(
            new CommandDefinition(
                "SELECT * FROM dbo.FN_GET_CATALOGO_REPORTES()",
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return rows.ToList();
    }

    public async Task<PrepararSolicitudReporteResponse> PrepararSolicitudReporteAsync(
        PrepararSolicitudReporteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FechaFin.Date < request.FechaInicio.Date)
        {
            return new PrepararSolicitudReporteResponse
            {
                Valido = false,
                Mensaje = "La fecha fin no puede ser anterior a la fecha de inicio."
            };
        }

        const int maxDias = 365;
        var dias = (request.FechaFin.Date - request.FechaInicio.Date).Days + 1;
        if (dias > maxDias)
        {
            return new PrepararSolicitudReporteResponse
            {
                Valido = false,
                Mensaje = $"El rango no puede superar {maxDias} días."
            };
        }

        var formato = (request.Formato ?? string.Empty).Trim();
        if (formato.Length == 0)
        {
            return new PrepararSolicitudReporteResponse
            {
                Valido = false,
                Mensaje = "Indique formato (csv o xlsx)."
            };
        }

        var fmt = formato.Equals("xlsx", StringComparison.OrdinalIgnoreCase) ? "xlsx" :
            formato.Equals("csv", StringComparison.OrdinalIgnoreCase) ? "csv" : null;
        if (fmt is null)
        {
            return new PrepararSolicitudReporteResponse
            {
                Valido = false,
                Mensaje = "Formato no soportado. Use csv o xlsx."
            };
        }

        if (request.IdReporte <= 0)
        {
            return new PrepararSolicitudReporteResponse
            {
                Valido = false,
                Mensaje = "IdReporte debe ser mayor a cero."
            };
        }

        var catalog = await GetReportesCatalogAsync(cancellationToken).ConfigureAwait(false);
        var item = catalog.FirstOrDefault(c => c.ID == request.IdReporte);
        if (item is null)
        {
            return new PrepararSolicitudReporteResponse
            {
                Valido = false,
                Mensaje = "El tipo de reporte no existe en el catálogo actual."
            };
        }

        return new PrepararSolicitudReporteResponse
        {
            Valido = true,
            DescripcionReporte = item.Descripcion,
            RangoDias = dias,
            FormatoNormalizado = fmt,
            Mensaje = "Parámetros válidos. Puede generar el reporte desde Monitor Blazor (/reportes)."
        };
    }

    public async Task<int?> InsertReporteHistoricoAsync(
        string nombreArchivo,
        string extension,
        string ruta,
        string usuario,
        int idReporte,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var id = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "dbo.SP_Insertar_ReportesHistoricos",
                new
                {
                    NOMBRE_ARCHIVO = nombreArchivo,
                    EXTENSION = extension.Trim().TrimStart('.'),
                    RUTA = ruta,
                    USUARIO = usuario,
                    ID_REPORTE = idReporte
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        return id is > 0 ? id : null;
    }

    public async Task<IReadOnlyList<ReporteHistoricoRowDto>> GetReportesHistoricosAsync(
        ReportesHistoricosRequest request,
        CancellationToken cancellationToken = default)
    {
        var cantidad = Math.Clamp(request.CantidadFila, 1, 500);
        var ignorar = Math.Max(0, request.Ignorar);
        var filtro = request.Filtro ?? string.Empty;
        var orden = string.IsNullOrWhiteSpace(request.Orden) ? "FECHA_CREACION" : request.Orden.Trim();
        var dir = string.IsNullOrWhiteSpace(request.Dir) ? "desc" : request.Dir.Trim();

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var sqlRows = await connection.QueryAsync<ReporteHistoricoSqlRow>(
            new CommandDefinition(
                "dbo.SP_GET_Reportes_Historicos",
                new
                {
                    FECHA_INICIO = request.FechaInicio,
                    FECHA_FIN = request.FechaFin,
                    Ignorar = ignorar,
                    Cantidad_Fila = cantidad,
                    Filtro = filtro,
                    Orden = orden,
                    Dir = dir
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var list = new List<ReporteHistoricoRowDto>();
        foreach (var r in sqlRows)
        {
            list.Add(new ReporteHistoricoRowDto
            {
                IdReportesHistorico = r.ID_REPORTES_HISTORICO,
                NombreArchivo = r.NOMBRE_ARCHIVO ?? string.Empty,
                Extension = r.EXTENSION ?? string.Empty,
                Usuario = r.USUARIO ?? string.Empty,
                IdReporte = r.ID_REPORTE,
                FechaCreacion = r.FECHA_CREACION
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<Dictionary<string, string>>> GetReporteContadoresAsync(
        ReporteContadoresRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FechaFin.Date < request.FechaInicio.Date)
            return Array.Empty<Dictionary<string, string>>();

        var cantidad = Math.Clamp(request.CantidadFila, 1, 500);
        var ignorar = Math.Max(0, request.Ignorar);
        var filtro = request.Filtro ?? string.Empty;
        var orden = string.IsNullOrWhiteSpace(request.Orden) ? "ID" : request.Orden.Trim();
        var dir = string.IsNullOrWhiteSpace(request.Dir) ? "ASC" : request.Dir.Trim();

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync(
            new CommandDefinition(
                "dbo.SP_GET_REPORTEDECONTADORES",
                new
                {
                    FechaInicio = request.FechaInicio.Date,
                    FechaFin = request.FechaFin.Date,
                    Ignorar = ignorar,
                    Cantidad_Fila = cantidad,
                    Filtro = filtro,
                    Orden = orden,
                    Dir = dir
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var list = new List<Dictionary<string, string>>();
        foreach (var row in rows)
        {
            if (row is not IDictionary<string, object> dict)
                continue;

            var flat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in dict)
                flat[kv.Key] = kv.Value?.ToString() ?? string.Empty;

            list.Add(flat);
        }

        return list;
    }

    private static readonly HashSet<string> ReporteExportProcedures = new(StringComparer.OrdinalIgnoreCase)
    {
        "dbo.SP_GET_REPORTE_EP_ACTIVAS",
        "dbo.SP_GET_REPORTE_TRANSACCIONES_BITACORA",
        "dbo.SP_GET_REPORTE_TRANSACCIONES",
        "dbo.SP_GET_REPORTE_TRANSACCIONES_POR_EQUIPO",
        "dbo.SP_GET_REPORTE_CAMPANAS_MKT",
        "dbo.SP_GET_REPORTE_CIERRE_CAJA",
        "dbo.SP_GET_REPORTE_CONTADORES"
    };

    public async Task<IReadOnlyList<Dictionary<string, string>>> GetReporteExportPaginadoAsync(
        ReporteExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var sp = (request.StoredProcedure ?? string.Empty).Trim();
        if (!ReporteExportProcedures.Contains(sp))
            throw new ArgumentException($"Procedimiento de reporte no permitido: {sp}", nameof(request));

        if (request.FechaFin.Date < request.FechaInicio.Date)
            return Array.Empty<Dictionary<string, string>>();

        var cantidad = Math.Clamp(request.CantidadFila, 1, 500);
        var ignorar = Math.Max(0, request.Ignorar);
        var filtro = request.Filtro ?? string.Empty;
        var orden = string.IsNullOrWhiteSpace(request.Orden) ? "FECHA" : request.Orden.Trim();
        var dir = string.IsNullOrWhiteSpace(request.Dir) ? "ASC" : request.Dir.Trim();

        var parameters = new DynamicParameters();
        parameters.Add("FechaInicio", request.FechaInicio.Date);
        parameters.Add("FechaFin", request.FechaFin.Date);
        parameters.Add("Ignorar", ignorar);
        parameters.Add("Cantidad_Fila", cantidad);
        parameters.Add("Filtro", filtro);
        parameters.Add("Orden", orden);
        parameters.Add("Dir", dir);

        if (sp.Contains("TRANSACCIONES_BITACORA", StringComparison.OrdinalIgnoreCase))
        {
            parameters.Add("SoloError", request.SoloError == true ? 1 : 0);
            parameters.Add("EP", string.IsNullOrWhiteSpace(request.Ep) ? null : request.Ep.Trim());
        }
        else if (sp.Contains("TRANSACCIONES_POR_EQUIPO", StringComparison.OrdinalIgnoreCase))
        {
            parameters.Add("EpsCsv", string.IsNullOrWhiteSpace(request.EpsCsv) ? null : request.EpsCsv.Trim());
            parameters.Add("EP", string.IsNullOrWhiteSpace(request.Ep) ? null : request.Ep.Trim());
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var rows = await connection.QueryAsync(
            new CommandDefinition(
                sp,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        var list = new List<Dictionary<string, string>>();
        foreach (var row in rows)
        {
            if (row is not IDictionary<string, object> dict)
                continue;

            var flat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in dict)
                flat[kv.Key] = kv.Value?.ToString() ?? string.Empty;

            list.Add(flat);
        }

        return list;
    }

    public async Task<ProcedureResultDto?> StartDetalleEquipoSesionAsync(
        DetalleEquipoSesionRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ProcedureResultDto>(
            new CommandDefinition(
                "dbo.SP_DETALLE_EQUIPO_START",
                new
                {
                    ID_SESION = request.IdSesion,
                    ID_ATM = request.IdAtm,
                    USUARIO = request.Usuario
                },
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    public async Task<ProcedureResultDto?> PingDetalleEquipoSesionAsync(
        DetalleEquipoPingRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ProcedureResultDto>(
            new CommandDefinition(
                "dbo.SP_DETALLE_EQUIPO_PING",
                new { ID_SESION = request.IdSesion },
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    public async Task<ProcedureResultDto?> StopDetalleEquipoSesionAsync(
        DetalleEquipoStopRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ProcedureResultDto>(
            new CommandDefinition(
                "dbo.SP_DETALLE_EQUIPO_STOP",
                new { ID_SESION = request.IdSesion },
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    public async Task<ProcedureResultDto?> InsertDetalleEquipoPerfAsync(
        PerfMetricaInsertRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ProcedureResultDto>(
            new CommandDefinition(
                "dbo.SP_DETALLE_EQUIPO_INSERT_PERF",
                new
                {
                    ID_SESION = request.IdSesion,
                    ID_ATM = request.IdAtm,
                    CPU_PCT = request.CpuPct,
                    RAM_PCT = request.RamPct,
                    RAM_USADA_MB = request.RamUsadaMb,
                    RAM_TOTAL_MB = request.RamTotalMb
                },
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PerfMetricaPunto>> GetDetalleEquipoPerfSerieAsync(
        PerfMetricaSerieRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        var rows = await connection.QueryAsync<PerfMetricaPunto>(
            new CommandDefinition(
                "dbo.SP_DETALLE_EQUIPO_GET_PERF_SERIE",
                new
                {
                    ID_ATM = request.IdAtm,
                    MINUTOS = request.Minutos,
                    BUCKET_SEG = request.BucketSeg
                },
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);

        return rows.ToList();
    }

    public async Task<ProcedureResultDto?> UpsertDetalleEquipoSoDdFromAgentAsync(
        DetalleEquipoSoSyncSubmitRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.IdAtm))
            return null;

        await using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ProcedureResultDto>(
            new CommandDefinition(
                "dbo.SP_DETALLE_EQUIPO_UPSERT_SO_DD_ON_START",
                new
                {
                    ID_ATM = request.IdAtm.Trim(),
                    ID_SESION = request.IdSesion,
                    SISTEMA_OPERATIVO = request.SistemaOperativo,
                    MEMORIA_RAM = request.MemoriaRamGb,
                    IDIOMA = request.Idioma,
                    RESOLUCION = request.Resolucion,
                    PROCESADOR = request.Procesador,
                    NOMBRE_DE_WINDOWS = request.NombreDeWindows,
                    ZONA_HORARIA = request.ZonaHoraria,
                    NOMBRE_EQUIPO_SO = request.NombreEquipoSo,
                    UNIDAD_DISCO_DURO = request.UnidadDiscoDuro,
                    TAMANHO_DISCO_DURO = request.TamanhoDiscoDuro,
                    ESPACIO_LIBRE_DD = request.EspacioLibreDd
                },
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    private static DateTime? ParseFallaFilterDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var t = raw.Trim();
        if (DateTime.TryParseExact(t, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d.Date;
        if (DateTime.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out d))
            return d.Date;
        if (DateTime.TryParse(t, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out d))
            return d.Date;

        return null;
    }

    private sealed class ReporteHistoricoSqlRow
    {
        public int ID_REPORTES_HISTORICO { get; set; }
        public string? NOMBRE_ARCHIVO { get; set; }
        public string? EXTENSION { get; set; }
        public string? USUARIO { get; set; }
        public int ID_REPORTE { get; set; }
        public DateTime FECHA_CREACION { get; set; }
    }
}
