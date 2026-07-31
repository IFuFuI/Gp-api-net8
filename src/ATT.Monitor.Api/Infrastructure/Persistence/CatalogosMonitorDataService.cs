using System.Data;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.CatalogosMonitor;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ATT.Monitor.Api.Infrastructure.Persistence;

/// <summary>CRUD directo sobre tablas <c>dbo.C_*</c> del monitor (consultas parametrizadas, orden por lista blanca).</summary>
public sealed class CatalogosMonitorDataService(IConfiguration configuration) : ICatalogosMonitorData
{
    private string ConnectionString =>
        configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:SqlServer.");

    private static readonly IReadOnlyDictionary<string, string> RegionOrder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ID"] = "Id",
        ["REGION"] = "Region",
        ["FECHA_CREACION"] = "FechaCreacion",
        ["FECHA_ULTIMA_MODIFICACION"] = "FechaUltimaModificacion"
    };

    private static readonly IReadOnlyDictionary<string, string> RutaOrder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ID"] = "Id",
        ["DESCRIPCION"] = "Descripcion",
        ["RUTA"] = "Ruta",
        ["NOMBRE_ARCHIVO"] = "NombreArchivo",
        ["FECHA_CREACION"] = "FechaCreacion",
        ["FECHA_ULTIMA_MODIFICACION"] = "FechaUltimaModificacion"
    };

    private static readonly IReadOnlyDictionary<string, string> AlertaOrder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ID"] = "Id",
        ["DESCRIPCION"] = "Descripcion",
        ["SEVERIDAD"] = "Severidad",
        ["ACTIVAR_ALERTA"] = "ActivarAlerta",
        ["FECHA_CREACION"] = "FechaCreacion",
        ["FECHA_ULTIMA_MODIFICACION"] = "FechaUltimaModificacion"
    };

    private static readonly IReadOnlyDictionary<string, string> StatusTxOrder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ID"] = "Id",
        ["DESCRIPCION"] = "Descripcion",
        ["FECHA_CREACION"] = "FechaCreacion",
        ["FECHA_ULTIMA_MODIFICACION"] = "FechaUltimaModificacion"
    };

    private static readonly IReadOnlyDictionary<string, string> DetalleTxOrder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ID"] = "Id",
        ["NOMBRE_DETALLE"] = "NombreDetalle",
        ["TIPO_DATO"] = "TipoDato",
        ["FECHA_CREACION"] = "FechaCreacion",
        ["FECHA_ULTIMA_MODIFICACION"] = "FechaUltimaModificacion"
    };

    private static readonly IReadOnlyDictionary<string, string> AtributoTxOrder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ID"] = "Id",
        ["DESCRIPCION"] = "Descripcion",
        ["TIPO_DATO"] = "TipoDato",
        ["FECHA_CREACION"] = "FechaCreacion",
        ["FECHA_ULTIMA_MODIFICACION"] = "FechaUltimaModificacion"
    };

    private static readonly IReadOnlyDictionary<string, string> DispositivoOrder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ID"] = "Id",
        ["NOMBRE_DISPOSITIVO"] = "NombreDispositivo",
        ["FECHA_CREACION"] = "FechaCreacion",
        ["FECHA_ULTIMA_MODIFICACION"] = "FechaUltimaModificacion"
    };

    private static readonly IReadOnlyDictionary<string, string> CatalogLocationOrder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ID"] = "Id",
        ["LOCATION_NAME"] = "LocationName",
        ["ID_REGION"] = "IdRegion",
        ["REGION_NOMBRE"] = "RegionNombre",
        ["UBICACION"] = "Ubicacion",
        ["ESTADO"] = "Estado"
    };

    private static void Clamp(MonitorCatalogListRequest request)
    {
        if (request.PageNumber < 1)
            request.PageNumber = 1;
        if (request.PageSize < 1)
            request.PageSize = 10;
        if (request.PageSize > 200)
            request.PageSize = 200;
    }

    private static string NormalizeDir(string? dir)
        => string.Equals(dir?.Trim(), "DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

    private static string ResolveOrder(IReadOnlyDictionary<string, string> map, string? orderBy, string defaultKey)
    {
        var key = string.IsNullOrWhiteSpace(orderBy) ? defaultKey : orderBy.Trim();
        return map.TryGetValue(key, out var col) ? col : map[defaultKey];
    }

    #region C_REGION

    public async Task<MonitorCatalogPageResult<CRegionRow>> ListCRegionAsync(
        MonitorCatalogListRequest request, CancellationToken cancellationToken = default)
    {
        Clamp(request);
        var buscar = string.IsNullOrWhiteSpace(request.Buscar) ? null : request.Buscar.Trim();
        var offset = (request.PageNumber - 1) * request.PageSize;
        var orderCol = ResolveOrder(RegionOrder, request.OrderBy, "ID");
        var orderDir = NormalizeDir(request.OrderDir);

        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            SELECT x.Id, x.Region, x.FechaCreacion, x.FechaUltimaModificacion, x.TotalFiltrados
            FROM (
                SELECT r.ID AS Id, r.REGION AS Region, r.FECHA_CREACION AS FechaCreacion,
                       r.FECHA_ULTIMA_MODIFICACION AS FechaUltimaModificacion,
                       COUNT(*) OVER() AS TotalFiltrados
                FROM dbo.C_REGION r
                WHERE (@Buscar IS NULL OR r.REGION LIKE N'%' + @Buscar + N'%')
            ) x
            ORDER BY
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'ASC' THEN x.Id END ASC,
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'DESC' THEN x.Id END DESC,
                CASE WHEN @OrderBy = 'Region' AND @OrderDir = 'ASC' THEN x.Region END ASC,
                CASE WHEN @OrderBy = 'Region' AND @OrderDir = 'DESC' THEN x.Region END DESC,
                CASE WHEN @OrderBy = 'FechaCreacion' AND @OrderDir = 'ASC' THEN x.FechaCreacion END ASC,
                CASE WHEN @OrderBy = 'FechaCreacion' AND @OrderDir = 'DESC' THEN x.FechaCreacion END DESC,
                CASE WHEN @OrderBy = 'FechaUltimaModificacion' AND @OrderDir = 'ASC' THEN x.FechaUltimaModificacion END ASC,
                CASE WHEN @OrderBy = 'FechaUltimaModificacion' AND @OrderDir = 'DESC' THEN x.FechaUltimaModificacion END DESC,
                x.Id ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var p = new DynamicParameters();
        p.Add("@Buscar", buscar, DbType.String, size: 150);
        p.Add("@Offset", offset, DbType.Int32);
        p.Add("@PageSize", request.PageSize, DbType.Int32);
        p.Add("@OrderBy", orderCol, DbType.String, size: 50);
        p.Add("@OrderDir", orderDir, DbType.String, size: 4);

        var rows = (await connection.QueryAsync<CRegionAgg>(
            new CommandDefinition(sql, p, cancellationToken: cancellationToken, commandType: CommandType.Text))).AsList();

        var total = rows.FirstOrDefault()?.TotalFiltrados ?? 0;
        var items = rows.Select(r => new CRegionRow
        {
            Id = r.Id,
            Region = r.Region,
            FechaCreacion = r.FechaCreacion,
            FechaUltimaModificacion = r.FechaUltimaModificacion
        }).ToList();

        return new MonitorCatalogPageResult<CRegionRow> { Rows = items, TotalFiltrados = total };
    }

    public async Task<int> CreateCRegionAsync(CRegionCreateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = "INSERT INTO dbo.C_REGION (REGION) OUTPUT INSERTED.ID AS Id VALUES (@Region);";
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Region = request.Region.Trim() }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<bool> UpdateCRegionAsync(CRegionUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            UPDATE dbo.C_REGION
            SET REGION = @Region, FECHA_ULTIMA_MODIFICACION = SYSDATETIME()
            WHERE ID = @Id
            """;
        var n = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { request.Id, Region = request.Region.Trim() }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    public async Task<bool> DeleteCRegionAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var n = await connection.ExecuteAsync(
            new CommandDefinition("DELETE FROM dbo.C_REGION WHERE ID = @Id", new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    #endregion

    #region C_RUTA

    public async Task<MonitorCatalogPageResult<CRutaRow>> ListCRutaAsync(
        MonitorCatalogListRequest request, CancellationToken cancellationToken = default)
    {
        Clamp(request);
        var buscar = string.IsNullOrWhiteSpace(request.Buscar) ? null : request.Buscar.Trim();
        var offset = (request.PageNumber - 1) * request.PageSize;
        var orderCol = ResolveOrder(RutaOrder, request.OrderBy, "ID");
        var orderDir = NormalizeDir(request.OrderDir);

        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            SELECT x.Id, x.Descripcion, x.Ruta, x.NombreArchivo, x.FechaCreacion, x.FechaUltimaModificacion, x.TotalFiltrados
            FROM (
                SELECT r.ID AS Id, r.DESCRIPCION AS Descripcion, r.RUTA AS Ruta, r.NOMBRE_ARCHIVO AS NombreArchivo,
                       r.FECHA_CREACION AS FechaCreacion, r.FECHA_ULTIMA_MODIFICACION AS FechaUltimaModificacion,
                       COUNT(*) OVER() AS TotalFiltrados
                FROM dbo.C_RUTA r
                WHERE (@Buscar IS NULL OR r.DESCRIPCION LIKE N'%' + @Buscar + N'%'
                    OR r.RUTA LIKE N'%' + @Buscar + N'%'
                    OR r.NOMBRE_ARCHIVO LIKE N'%' + @Buscar + N'%')
            ) x
            ORDER BY
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'ASC' THEN x.Id END ASC,
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'DESC' THEN x.Id END DESC,
                CASE WHEN @OrderBy = 'Descripcion' AND @OrderDir = 'ASC' THEN x.Descripcion END ASC,
                CASE WHEN @OrderBy = 'Descripcion' AND @OrderDir = 'DESC' THEN x.Descripcion END DESC,
                CASE WHEN @OrderBy = 'Ruta' AND @OrderDir = 'ASC' THEN x.Ruta END ASC,
                CASE WHEN @OrderBy = 'Ruta' AND @OrderDir = 'DESC' THEN x.Ruta END DESC,
                CASE WHEN @OrderBy = 'NombreArchivo' AND @OrderDir = 'ASC' THEN x.NombreArchivo END ASC,
                CASE WHEN @OrderBy = 'NombreArchivo' AND @OrderDir = 'DESC' THEN x.NombreArchivo END DESC,
                CASE WHEN @OrderBy = 'FechaCreacion' AND @OrderDir = 'ASC' THEN x.FechaCreacion END ASC,
                CASE WHEN @OrderBy = 'FechaCreacion' AND @OrderDir = 'DESC' THEN x.FechaCreacion END DESC,
                CASE WHEN @OrderBy = 'FechaUltimaModificacion' AND @OrderDir = 'ASC' THEN x.FechaUltimaModificacion END ASC,
                CASE WHEN @OrderBy = 'FechaUltimaModificacion' AND @OrderDir = 'DESC' THEN x.FechaUltimaModificacion END DESC,
                x.Id ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var p = new DynamicParameters();
        p.Add("@Buscar", buscar, DbType.String, size: 200);
        p.Add("@Offset", offset, DbType.Int32);
        p.Add("@PageSize", request.PageSize, DbType.Int32);
        p.Add("@OrderBy", orderCol, DbType.String, size: 50);
        p.Add("@OrderDir", orderDir, DbType.String, size: 4);

        var rows = (await connection.QueryAsync<CRutaAgg>(
            new CommandDefinition(sql, p, cancellationToken: cancellationToken, commandType: CommandType.Text))).AsList();

        var total = rows.FirstOrDefault()?.TotalFiltrados ?? 0;
        var items = rows.Select(r => new CRutaRow
        {
            Id = r.Id,
            Descripcion = r.Descripcion,
            Ruta = r.Ruta,
            NombreArchivo = r.NombreArchivo,
            FechaCreacion = r.FechaCreacion,
            FechaUltimaModificacion = r.FechaUltimaModificacion
        }).ToList();

        return new MonitorCatalogPageResult<CRutaRow> { Rows = items, TotalFiltrados = total };
    }

    public async Task<int> CreateCRutaAsync(CRutaCreateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            INSERT INTO dbo.C_RUTA (DESCRIPCION, RUTA, NOMBRE_ARCHIVO)
            OUTPUT INSERTED.ID AS Id
            VALUES (@Descripcion, @Ruta, @NombreArchivo)
            """;
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new
            {
                Descripcion = request.Descripcion.Trim(),
                Ruta = request.Ruta.Trim(),
                NombreArchivo = request.NombreArchivo.Trim()
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<bool> UpdateCRutaAsync(CRutaUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            UPDATE dbo.C_RUTA
            SET DESCRIPCION = @Descripcion, RUTA = @Ruta, NOMBRE_ARCHIVO = @NombreArchivo,
                FECHA_ULTIMA_MODIFICACION = SYSDATETIME()
            WHERE ID = @Id
            """;
        var n = await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                request.Id,
                Descripcion = request.Descripcion.Trim(),
                Ruta = request.Ruta.Trim(),
                NombreArchivo = request.NombreArchivo.Trim()
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    public async Task<bool> DeleteCRutaAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var n = await connection.ExecuteAsync(
            new CommandDefinition("DELETE FROM dbo.C_RUTA WHERE ID = @Id", new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    #endregion

    #region C_ALERTA

    public async Task<MonitorCatalogPageResult<CAlertaRow>> ListCAlertaAsync(
        MonitorCatalogListRequest request, CancellationToken cancellationToken = default)
    {
        Clamp(request);
        var buscar = string.IsNullOrWhiteSpace(request.Buscar) ? null : request.Buscar.Trim();
        var offset = (request.PageNumber - 1) * request.PageSize;
        var orderCol = ResolveOrder(AlertaOrder, request.OrderBy, "ID");
        var orderDir = NormalizeDir(request.OrderDir);

        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            SELECT x.Id, x.Descripcion, x.Severidad, x.ActivarAlerta, x.FechaCreacion, x.FechaUltimaModificacion, x.TotalFiltrados
            FROM (
                SELECT a.ID AS Id, a.DESCRIPCION AS Descripcion, a.SEVERIDAD AS Severidad,
                       CAST(a.ACTIVAR_ALERTA AS bit) AS ActivarAlerta,
                       a.FECHA_CREACION AS FechaCreacion, a.FECHA_ULTIMA_MODIFICACION AS FechaUltimaModificacion,
                       COUNT(*) OVER() AS TotalFiltrados
                FROM dbo.C_ALERTA a
                WHERE (@Buscar IS NULL OR a.DESCRIPCION LIKE N'%' + @Buscar + N'%'
                    OR CAST(a.SEVERIDAD AS nvarchar(20)) LIKE N'%' + @Buscar + N'%')
            ) x
            ORDER BY
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'ASC' THEN x.Id END ASC,
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'DESC' THEN x.Id END DESC,
                CASE WHEN @OrderBy = 'Descripcion' AND @OrderDir = 'ASC' THEN x.Descripcion END ASC,
                CASE WHEN @OrderBy = 'Descripcion' AND @OrderDir = 'DESC' THEN x.Descripcion END DESC,
                CASE WHEN @OrderBy = 'Severidad' AND @OrderDir = 'ASC' THEN x.Severidad END ASC,
                CASE WHEN @OrderBy = 'Severidad' AND @OrderDir = 'DESC' THEN x.Severidad END DESC,
                CASE WHEN @OrderBy = 'ActivarAlerta' AND @OrderDir = 'ASC' THEN x.ActivarAlerta END ASC,
                CASE WHEN @OrderBy = 'ActivarAlerta' AND @OrderDir = 'DESC' THEN x.ActivarAlerta END DESC,
                CASE WHEN @OrderBy = 'FechaCreacion' AND @OrderDir = 'ASC' THEN x.FechaCreacion END ASC,
                CASE WHEN @OrderBy = 'FechaCreacion' AND @OrderDir = 'DESC' THEN x.FechaCreacion END DESC,
                CASE WHEN @OrderBy = 'FechaUltimaModificacion' AND @OrderDir = 'ASC' THEN x.FechaUltimaModificacion END ASC,
                CASE WHEN @OrderBy = 'FechaUltimaModificacion' AND @OrderDir = 'DESC' THEN x.FechaUltimaModificacion END DESC,
                x.Id ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var p = new DynamicParameters();
        p.Add("@Buscar", buscar, DbType.String, size: 255);
        p.Add("@Offset", offset, DbType.Int32);
        p.Add("@PageSize", request.PageSize, DbType.Int32);
        p.Add("@OrderBy", orderCol, DbType.String, size: 50);
        p.Add("@OrderDir", orderDir, DbType.String, size: 4);

        var rows = (await connection.QueryAsync<CAlertaAgg>(
            new CommandDefinition(sql, p, cancellationToken: cancellationToken, commandType: CommandType.Text))).AsList();

        var total = rows.FirstOrDefault()?.TotalFiltrados ?? 0;
        var items = rows.Select(r => new CAlertaRow
        {
            Id = r.Id,
            Descripcion = r.Descripcion,
            Severidad = r.Severidad,
            ActivarAlerta = r.ActivarAlerta,
            FechaCreacion = r.FechaCreacion,
            FechaUltimaModificacion = r.FechaUltimaModificacion
        }).ToList();

        return new MonitorCatalogPageResult<CAlertaRow> { Rows = items, TotalFiltrados = total };
    }

    public async Task<int> CreateCAlertaAsync(CAlertaCreateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            INSERT INTO dbo.C_ALERTA (DESCRIPCION, SEVERIDAD, ACTIVAR_ALERTA)
            OUTPUT INSERTED.ID AS Id
            VALUES (@Descripcion, @Severidad, @ActivarAlerta)
            """;
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new
            {
                Descripcion = request.Descripcion.Trim(),
                request.Severidad,
                request.ActivarAlerta
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<bool> UpdateCAlertaAsync(CAlertaUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            UPDATE dbo.C_ALERTA
            SET DESCRIPCION = @Descripcion, SEVERIDAD = @Severidad, ACTIVAR_ALERTA = @ActivarAlerta,
                FECHA_ULTIMA_MODIFICACION = SYSDATETIME()
            WHERE ID = @Id
            """;
        var n = await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                request.Id,
                Descripcion = request.Descripcion.Trim(),
                request.Severidad,
                request.ActivarAlerta
            }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    public async Task<bool> DeleteCAlertaAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var n = await connection.ExecuteAsync(
            new CommandDefinition("DELETE FROM dbo.C_ALERTA WHERE ID = @Id", new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    #endregion

    #region C_STATUS_TRANSACCION

    public async Task<MonitorCatalogPageResult<CStatusTransaccionRow>> ListCStatusTransaccionAsync(
        MonitorCatalogListRequest request, CancellationToken cancellationToken = default)
    {
        Clamp(request);
        var buscar = string.IsNullOrWhiteSpace(request.Buscar) ? null : request.Buscar.Trim();
        var offset = (request.PageNumber - 1) * request.PageSize;
        var orderCol = ResolveOrder(StatusTxOrder, request.OrderBy, "ID");
        var orderDir = NormalizeDir(request.OrderDir);

        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            SELECT x.Id, x.Descripcion, x.FechaCreacion, x.FechaUltimaModificacion, x.TotalFiltrados
            FROM (
                SELECT s.ID AS Id, s.DESCRIPCION AS Descripcion,
                       s.FECHA_CREACION AS FechaCreacion, s.FECHA_ULTIMA_MODIFICACION AS FechaUltimaModificacion,
                       COUNT(*) OVER() AS TotalFiltrados
                FROM dbo.C_STATUS_TRANSACCION s
                WHERE (@Buscar IS NULL OR s.DESCRIPCION LIKE N'%' + @Buscar + N'%')
            ) x
            ORDER BY
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'ASC' THEN x.Id END ASC,
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'DESC' THEN x.Id END DESC,
                CASE WHEN @OrderBy = 'Descripcion' AND @OrderDir = 'ASC' THEN x.Descripcion END ASC,
                CASE WHEN @OrderBy = 'Descripcion' AND @OrderDir = 'DESC' THEN x.Descripcion END DESC,
                CASE WHEN @OrderBy = 'FechaCreacion' AND @OrderDir = 'ASC' THEN x.FechaCreacion END ASC,
                CASE WHEN @OrderBy = 'FechaCreacion' AND @OrderDir = 'DESC' THEN x.FechaCreacion END DESC,
                CASE WHEN @OrderBy = 'FechaUltimaModificacion' AND @OrderDir = 'ASC' THEN x.FechaUltimaModificacion END ASC,
                CASE WHEN @OrderBy = 'FechaUltimaModificacion' AND @OrderDir = 'DESC' THEN x.FechaUltimaModificacion END DESC,
                x.Id ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var p = new DynamicParameters();
        p.Add("@Buscar", buscar, DbType.String, size: 200);
        p.Add("@Offset", offset, DbType.Int32);
        p.Add("@PageSize", request.PageSize, DbType.Int32);
        p.Add("@OrderBy", orderCol, DbType.String, size: 50);
        p.Add("@OrderDir", orderDir, DbType.String, size: 4);

        var rows = (await connection.QueryAsync<CStatusAgg>(
            new CommandDefinition(sql, p, cancellationToken: cancellationToken, commandType: CommandType.Text))).AsList();

        var total = rows.FirstOrDefault()?.TotalFiltrados ?? 0;
        var items = rows.Select(r => new CStatusTransaccionRow
        {
            Id = r.Id,
            Descripcion = r.Descripcion,
            FechaCreacion = r.FechaCreacion,
            FechaUltimaModificacion = r.FechaUltimaModificacion
        }).ToList();

        return new MonitorCatalogPageResult<CStatusTransaccionRow> { Rows = items, TotalFiltrados = total };
    }

    public async Task<int> CreateCStatusTransaccionAsync(CStatusTransaccionCreateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            INSERT INTO dbo.C_STATUS_TRANSACCION (DESCRIPCION)
            OUTPUT INSERTED.ID AS Id
            VALUES (@Descripcion)
            """;
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Descripcion = request.Descripcion.Trim() }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<bool> UpdateCStatusTransaccionAsync(CStatusTransaccionUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            UPDATE dbo.C_STATUS_TRANSACCION
            SET DESCRIPCION = @Descripcion, FECHA_ULTIMA_MODIFICACION = SYSDATETIME()
            WHERE ID = @Id
            """;
        var n = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { request.Id, Descripcion = request.Descripcion.Trim() }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    public async Task<bool> DeleteCStatusTransaccionAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var n = await connection.ExecuteAsync(
            new CommandDefinition("DELETE FROM dbo.C_STATUS_TRANSACCION WHERE ID = @Id", new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    #endregion

    #region C_DETALLE_TRANSACCION

    public async Task<MonitorCatalogPageResult<CDetalleTransaccionRow>> ListCDetalleTransaccionAsync(
        MonitorCatalogListRequest request, CancellationToken cancellationToken = default)
    {
        Clamp(request);
        var buscar = string.IsNullOrWhiteSpace(request.Buscar) ? null : request.Buscar.Trim();
        var offset = (request.PageNumber - 1) * request.PageSize;
        var orderCol = ResolveOrder(DetalleTxOrder, request.OrderBy, "ID");
        var orderDir = NormalizeDir(request.OrderDir);

        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            SELECT x.Id, x.NombreDetalle, x.TipoDato, x.FechaCreacion, x.FechaUltimaModificacion, x.TotalFiltrados
            FROM (
                SELECT d.ID AS Id, d.NOMBRE_DETALLE AS NombreDetalle, d.TIPO_DATO AS TipoDato,
                       d.FECHA_CREACION AS FechaCreacion, d.FECHA_ULTIMA_MODIFICACION AS FechaUltimaModificacion,
                       COUNT(*) OVER() AS TotalFiltrados
                FROM dbo.C_DETALLE_TRANSACCION d
                WHERE (@Buscar IS NULL OR d.NOMBRE_DETALLE LIKE N'%' + @Buscar + N'%'
                    OR CAST(d.TIPO_DATO AS nvarchar(20)) LIKE N'%' + @Buscar + N'%')
            ) x
            ORDER BY
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'ASC' THEN x.Id END ASC,
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'DESC' THEN x.Id END DESC,
                CASE WHEN @OrderBy = 'NombreDetalle' AND @OrderDir = 'ASC' THEN x.NombreDetalle END ASC,
                CASE WHEN @OrderBy = 'NombreDetalle' AND @OrderDir = 'DESC' THEN x.NombreDetalle END DESC,
                CASE WHEN @OrderBy = 'TipoDato' AND @OrderDir = 'ASC' THEN x.TipoDato END ASC,
                CASE WHEN @OrderBy = 'TipoDato' AND @OrderDir = 'DESC' THEN x.TipoDato END DESC,
                CASE WHEN @OrderBy = 'FechaCreacion' AND @OrderDir = 'ASC' THEN x.FechaCreacion END ASC,
                CASE WHEN @OrderBy = 'FechaCreacion' AND @OrderDir = 'DESC' THEN x.FechaCreacion END DESC,
                CASE WHEN @OrderBy = 'FechaUltimaModificacion' AND @OrderDir = 'ASC' THEN x.FechaUltimaModificacion END ASC,
                CASE WHEN @OrderBy = 'FechaUltimaModificacion' AND @OrderDir = 'DESC' THEN x.FechaUltimaModificacion END DESC,
                x.Id ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var p = new DynamicParameters();
        p.Add("@Buscar", buscar, DbType.String, size: 200);
        p.Add("@Offset", offset, DbType.Int32);
        p.Add("@PageSize", request.PageSize, DbType.Int32);
        p.Add("@OrderBy", orderCol, DbType.String, size: 50);
        p.Add("@OrderDir", orderDir, DbType.String, size: 4);

        var rows = (await connection.QueryAsync<CDetalleAgg>(
            new CommandDefinition(sql, p, cancellationToken: cancellationToken, commandType: CommandType.Text))).AsList();

        var total = rows.FirstOrDefault()?.TotalFiltrados ?? 0;
        var items = rows.Select(r => new CDetalleTransaccionRow
        {
            Id = r.Id,
            NombreDetalle = r.NombreDetalle,
            TipoDato = r.TipoDato,
            FechaCreacion = r.FechaCreacion,
            FechaUltimaModificacion = r.FechaUltimaModificacion
        }).ToList();

        return new MonitorCatalogPageResult<CDetalleTransaccionRow> { Rows = items, TotalFiltrados = total };
    }

    public async Task<int> CreateCDetalleTransaccionAsync(CDetalleTransaccionCreateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            INSERT INTO dbo.C_DETALLE_TRANSACCION (NOMBRE_DETALLE, TIPO_DATO)
            OUTPUT INSERTED.ID AS Id
            VALUES (@NombreDetalle, @TipoDato)
            """;
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { NombreDetalle = request.NombreDetalle.Trim(), request.TipoDato }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<bool> UpdateCDetalleTransaccionAsync(CDetalleTransaccionUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            UPDATE dbo.C_DETALLE_TRANSACCION
            SET NOMBRE_DETALLE = @NombreDetalle, TIPO_DATO = @TipoDato, FECHA_ULTIMA_MODIFICACION = SYSDATETIME()
            WHERE ID = @Id
            """;
        var n = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { request.Id, NombreDetalle = request.NombreDetalle.Trim(), request.TipoDato }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    public async Task<bool> DeleteCDetalleTransaccionAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var n = await connection.ExecuteAsync(
            new CommandDefinition("DELETE FROM dbo.C_DETALLE_TRANSACCION WHERE ID = @Id", new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    #endregion

    #region C_ATRIBUTO_TRANSACCION

    public async Task<MonitorCatalogPageResult<CAtributoTransaccionRow>> ListCAtributoTransaccionAsync(
        MonitorCatalogListRequest request, CancellationToken cancellationToken = default)
    {
        Clamp(request);
        var buscar = string.IsNullOrWhiteSpace(request.Buscar) ? null : request.Buscar.Trim();
        var offset = (request.PageNumber - 1) * request.PageSize;
        var orderCol = ResolveOrder(AtributoTxOrder, request.OrderBy, "ID");
        var orderDir = NormalizeDir(request.OrderDir);

        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            SELECT x.Id, x.Descripcion, x.TipoDato, x.FechaCreacion, x.FechaUltimaModificacion, x.TotalFiltrados
            FROM (
                SELECT a.ID AS Id, a.DESCRIPCION AS Descripcion, a.TIPO_DATO AS TipoDato,
                       a.FECHA_CREACION AS FechaCreacion, a.FECHA_ULTIMA_MODIFICACION AS FechaUltimaModificacion,
                       COUNT(*) OVER() AS TotalFiltrados
                FROM dbo.C_ATRIBUTO_TRANSACCION a
                WHERE (@Buscar IS NULL OR a.DESCRIPCION LIKE N'%' + @Buscar + N'%'
                    OR CAST(a.TIPO_DATO AS nvarchar(20)) LIKE N'%' + @Buscar + N'%')
            ) x
            ORDER BY
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'ASC' THEN x.Id END ASC,
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'DESC' THEN x.Id END DESC,
                CASE WHEN @OrderBy = 'Descripcion' AND @OrderDir = 'ASC' THEN x.Descripcion END ASC,
                CASE WHEN @OrderBy = 'Descripcion' AND @OrderDir = 'DESC' THEN x.Descripcion END DESC,
                CASE WHEN @OrderBy = 'TipoDato' AND @OrderDir = 'ASC' THEN x.TipoDato END ASC,
                CASE WHEN @OrderBy = 'TipoDato' AND @OrderDir = 'DESC' THEN x.TipoDato END DESC,
                CASE WHEN @OrderBy = 'FechaCreacion' AND @OrderDir = 'ASC' THEN x.FechaCreacion END ASC,
                CASE WHEN @OrderBy = 'FechaCreacion' AND @OrderDir = 'DESC' THEN x.FechaCreacion END DESC,
                CASE WHEN @OrderBy = 'FechaUltimaModificacion' AND @OrderDir = 'ASC' THEN x.FechaUltimaModificacion END ASC,
                CASE WHEN @OrderBy = 'FechaUltimaModificacion' AND @OrderDir = 'DESC' THEN x.FechaUltimaModificacion END DESC,
                x.Id ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var p = new DynamicParameters();
        p.Add("@Buscar", buscar, DbType.String, size: 200);
        p.Add("@Offset", offset, DbType.Int32);
        p.Add("@PageSize", request.PageSize, DbType.Int32);
        p.Add("@OrderBy", orderCol, DbType.String, size: 50);
        p.Add("@OrderDir", orderDir, DbType.String, size: 4);

        var rows = (await connection.QueryAsync<CAtributoAgg>(
            new CommandDefinition(sql, p, cancellationToken: cancellationToken, commandType: CommandType.Text))).AsList();

        var total = rows.FirstOrDefault()?.TotalFiltrados ?? 0;
        var items = rows.Select(r => new CAtributoTransaccionRow
        {
            Id = r.Id,
            Descripcion = r.Descripcion,
            TipoDato = r.TipoDato,
            FechaCreacion = r.FechaCreacion,
            FechaUltimaModificacion = r.FechaUltimaModificacion
        }).ToList();

        return new MonitorCatalogPageResult<CAtributoTransaccionRow> { Rows = items, TotalFiltrados = total };
    }

    public async Task<int> CreateCAtributoTransaccionAsync(CAtributoTransaccionCreateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            INSERT INTO dbo.C_ATRIBUTO_TRANSACCION (DESCRIPCION, TIPO_DATO)
            OUTPUT INSERTED.ID AS Id
            VALUES (@Descripcion, @TipoDato)
            """;
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Descripcion = request.Descripcion.Trim(), request.TipoDato }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<bool> UpdateCAtributoTransaccionAsync(CAtributoTransaccionUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            UPDATE dbo.C_ATRIBUTO_TRANSACCION
            SET DESCRIPCION = @Descripcion, TIPO_DATO = @TipoDato, FECHA_ULTIMA_MODIFICACION = SYSDATETIME()
            WHERE ID = @Id
            """;
        var n = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { request.Id, Descripcion = request.Descripcion.Trim(), request.TipoDato }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    public async Task<bool> DeleteCAtributoTransaccionAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var n = await connection.ExecuteAsync(
            new CommandDefinition("DELETE FROM dbo.C_ATRIBUTO_TRANSACCION WHERE ID = @Id", new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    #endregion

    #region C_DISPOSITIVO_ELIMINADO

    public async Task<MonitorCatalogPageResult<CDispositivoEliminadoRow>> ListCDispositivoEliminadoAsync(
        MonitorCatalogListRequest request, CancellationToken cancellationToken = default)
    {
        Clamp(request);
        var buscar = string.IsNullOrWhiteSpace(request.Buscar) ? null : request.Buscar.Trim();
        var offset = (request.PageNumber - 1) * request.PageSize;
        var orderCol = ResolveOrder(DispositivoOrder, request.OrderBy, "ID");
        var orderDir = NormalizeDir(request.OrderDir);

        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            SELECT x.Id, x.NombreDispositivo, x.FechaCreacion, x.FechaUltimaModificacion, x.TotalFiltrados
            FROM (
                SELECT d.ID AS Id, d.NOMBRE_DISPOSITIVO AS NombreDispositivo,
                       d.FECHA_CREACION AS FechaCreacion, d.FECHA_ULTIMA_MODIFICACION AS FechaUltimaModificacion,
                       COUNT(*) OVER() AS TotalFiltrados
                FROM dbo.C_DISPOSITIVO_ELIMINADO d
                WHERE (@Buscar IS NULL OR d.NOMBRE_DISPOSITIVO LIKE N'%' + @Buscar + N'%')
            ) x
            ORDER BY
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'ASC' THEN x.Id END ASC,
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'DESC' THEN x.Id END DESC,
                CASE WHEN @OrderBy = 'NombreDispositivo' AND @OrderDir = 'ASC' THEN x.NombreDispositivo END ASC,
                CASE WHEN @OrderBy = 'NombreDispositivo' AND @OrderDir = 'DESC' THEN x.NombreDispositivo END DESC,
                CASE WHEN @OrderBy = 'FechaCreacion' AND @OrderDir = 'ASC' THEN x.FechaCreacion END ASC,
                CASE WHEN @OrderBy = 'FechaCreacion' AND @OrderDir = 'DESC' THEN x.FechaCreacion END DESC,
                CASE WHEN @OrderBy = 'FechaUltimaModificacion' AND @OrderDir = 'ASC' THEN x.FechaUltimaModificacion END ASC,
                CASE WHEN @OrderBy = 'FechaUltimaModificacion' AND @OrderDir = 'DESC' THEN x.FechaUltimaModificacion END DESC,
                x.Id ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var p = new DynamicParameters();
        p.Add("@Buscar", buscar, DbType.String, size: 200);
        p.Add("@Offset", offset, DbType.Int32);
        p.Add("@PageSize", request.PageSize, DbType.Int32);
        p.Add("@OrderBy", orderCol, DbType.String, size: 50);
        p.Add("@OrderDir", orderDir, DbType.String, size: 4);

        var rows = (await connection.QueryAsync<CDispositivoAgg>(
            new CommandDefinition(sql, p, cancellationToken: cancellationToken, commandType: CommandType.Text))).AsList();

        var total = rows.FirstOrDefault()?.TotalFiltrados ?? 0;
        var items = rows.Select(r => new CDispositivoEliminadoRow
        {
            Id = r.Id,
            NombreDispositivo = r.NombreDispositivo,
            FechaCreacion = r.FechaCreacion,
            FechaUltimaModificacion = r.FechaUltimaModificacion
        }).ToList();

        return new MonitorCatalogPageResult<CDispositivoEliminadoRow> { Rows = items, TotalFiltrados = total };
    }

    public async Task<int> CreateCDispositivoEliminadoAsync(CDispositivoEliminadoCreateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            INSERT INTO dbo.C_DISPOSITIVO_ELIMINADO (NOMBRE_DISPOSITIVO)
            OUTPUT INSERTED.ID AS Id
            VALUES (@NombreDispositivo)
            """;
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { NombreDispositivo = request.NombreDispositivo.Trim() }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<bool> UpdateCDispositivoEliminadoAsync(CDispositivoEliminadoUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            UPDATE dbo.C_DISPOSITIVO_ELIMINADO
            SET NOMBRE_DISPOSITIVO = @NombreDispositivo, FECHA_ULTIMA_MODIFICACION = SYSDATETIME()
            WHERE ID = @Id
            """;
        var n = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { request.Id, NombreDispositivo = request.NombreDispositivo.Trim() }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    public async Task<bool> DeleteCDispositivoEliminadoAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var n = await connection.ExecuteAsync(
            new CommandDefinition("DELETE FROM dbo.C_DISPOSITIVO_ELIMINADO WHERE ID = @Id", new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    #endregion

    #region Catalog_Locations

    public async Task<MonitorCatalogPageResult<CatalogLocationRow>> ListCatalogLocationAsync(
        MonitorCatalogListRequest request, CancellationToken cancellationToken = default)
    {
        Clamp(request);
        var buscar = string.IsNullOrWhiteSpace(request.Buscar) ? null : request.Buscar.Trim();
        var offset = (request.PageNumber - 1) * request.PageSize;
        var orderCol = ResolveOrder(CatalogLocationOrder, request.OrderBy, "ID");
        var orderDir = NormalizeDir(request.OrderDir);

        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            SELECT x.Id, x.LocationName, x.IdRegion, x.RegionNombre, x.Ubicacion, x.Estado, x.TotalFiltrados
            FROM (
                SELECT l.Id, l.Location_Name AS LocationName, l.Id_Region AS IdRegion,
                       r.REGION AS RegionNombre, l.Ubicacion, l.estado AS Estado,
                       COUNT(*) OVER() AS TotalFiltrados
                FROM dbo.Catalog_Locations l
                INNER JOIN dbo.C_REGION r ON r.ID = l.Id_Region
                WHERE (@Buscar IS NULL
                    OR l.Location_Name LIKE N'%' + @Buscar + N'%'
                    OR l.Ubicacion LIKE N'%' + @Buscar + N'%'
                    OR l.estado LIKE N'%' + @Buscar + N'%'
                    OR r.REGION LIKE N'%' + @Buscar + N'%'
                    OR CAST(l.Id AS NVARCHAR(20)) = @Buscar
                    OR CAST(l.Id_Region AS NVARCHAR(20)) = @Buscar)
            ) x
            ORDER BY
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'ASC' THEN x.Id END ASC,
                CASE WHEN @OrderBy = 'Id' AND @OrderDir = 'DESC' THEN x.Id END DESC,
                CASE WHEN @OrderBy = 'LocationName' AND @OrderDir = 'ASC' THEN x.LocationName END ASC,
                CASE WHEN @OrderBy = 'LocationName' AND @OrderDir = 'DESC' THEN x.LocationName END DESC,
                CASE WHEN @OrderBy = 'IdRegion' AND @OrderDir = 'ASC' THEN x.IdRegion END ASC,
                CASE WHEN @OrderBy = 'IdRegion' AND @OrderDir = 'DESC' THEN x.IdRegion END DESC,
                CASE WHEN @OrderBy = 'RegionNombre' AND @OrderDir = 'ASC' THEN x.RegionNombre END ASC,
                CASE WHEN @OrderBy = 'RegionNombre' AND @OrderDir = 'DESC' THEN x.RegionNombre END DESC,
                CASE WHEN @OrderBy = 'Ubicacion' AND @OrderDir = 'ASC' THEN x.Ubicacion END ASC,
                CASE WHEN @OrderBy = 'Ubicacion' AND @OrderDir = 'DESC' THEN x.Ubicacion END DESC,
                CASE WHEN @OrderBy = 'Estado' AND @OrderDir = 'ASC' THEN x.Estado END ASC,
                CASE WHEN @OrderBy = 'Estado' AND @OrderDir = 'DESC' THEN x.Estado END DESC,
                x.Id ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var p = new DynamicParameters();
        p.Add("@Buscar", buscar, DbType.String, size: 250);
        p.Add("@Offset", offset, DbType.Int32);
        p.Add("@PageSize", request.PageSize, DbType.Int32);
        p.Add("@OrderBy", orderCol, DbType.String, size: 50);
        p.Add("@OrderDir", orderDir, DbType.String, size: 4);

        var rows = (await connection.QueryAsync<CatalogLocationAgg>(
            new CommandDefinition(sql, p, cancellationToken: cancellationToken, commandType: CommandType.Text))).AsList();

        var total = rows.FirstOrDefault()?.TotalFiltrados ?? 0;
        var items = rows.Select(r => new CatalogLocationRow
        {
            Id = r.Id,
            LocationName = r.LocationName,
            IdRegion = r.IdRegion,
            RegionNombre = r.RegionNombre,
            Ubicacion = r.Ubicacion,
            Estado = r.Estado
        }).ToList();

        return new MonitorCatalogPageResult<CatalogLocationRow> { Rows = items, TotalFiltrados = total };
    }

    public async Task<int> CreateCatalogLocationAsync(CatalogLocationCreateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var regionOk = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(1) FROM dbo.C_REGION WHERE ID = @IdRegion",
                new { request.IdRegion },
                transaction: tx,
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (regionOk == 0)
            throw new InvalidOperationException("La región indicada no existe.");

        var newId = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT ISNULL(MAX(Id), 0) + 1 FROM dbo.Catalog_Locations WITH (UPDLOCK, HOLDLOCK)",
                transaction: tx,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        const string insertSql = """
            INSERT INTO dbo.Catalog_Locations (Id, Location_Name, Id_Region, Ubicacion, estado)
            VALUES (@Id, @LocationName, @IdRegion, @Ubicacion, @Estado);
            """;
        await connection.ExecuteAsync(
            new CommandDefinition(
                insertSql,
                new
                {
                    Id = newId,
                    LocationName = request.LocationName.Trim(),
                    request.IdRegion,
                    Ubicacion = request.Ubicacion.Trim(),
                    Estado = request.Estado.Trim()
                },
                transaction: tx,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return newId;
    }

    public async Task<bool> UpdateCatalogLocationAsync(CatalogLocationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var regionOk = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(1) FROM dbo.C_REGION WHERE ID = @IdRegion",
                new { request.IdRegion },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (regionOk == 0)
            return false;

        const string sql = """
            UPDATE dbo.Catalog_Locations
            SET Location_Name = @LocationName,
                Id_Region = @IdRegion,
                Ubicacion = @Ubicacion,
                estado = @Estado
            WHERE Id = @Id
            """;
        var n = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    request.Id,
                    LocationName = request.LocationName.Trim(),
                    request.IdRegion,
                    Ubicacion = request.Ubicacion.Trim(),
                    Estado = request.Estado.Trim()
                },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0;
    }

    public async Task<(bool Deleted, string? ErrorMessage)> DeleteCatalogLocationAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var inUse = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(1) FROM dbo.Device_Configuration WHERE Id_Location = @Id",
                new { Id = id },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (inUse > 0)
        {
            return (false, inUse == 1
                ? "No se puede eliminar: hay 1 equipo asociado a esta localización."
                : $"No se puede eliminar: hay {inUse} equipos asociados a esta localización.");
        }

        var n = await connection.ExecuteAsync(
            new CommandDefinition(
                "DELETE FROM dbo.Catalog_Locations WHERE Id = @Id",
                new { Id = id },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return n > 0 ? (true, null) : (false, "No se encontró el registro.");
    }

    public async Task<IReadOnlyList<CEstadoComboRow>> ListCEstadosComboAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            SELECT e.ID_ESTADO AS Id, e.NOMBRE AS Nombre, e.HCKEY AS HcKey
            FROM dbo.C_ESTADOS e
            ORDER BY e.NOMBRE ASC
            """;
        var rows = await connection.QueryAsync<CEstadoComboRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken, commandType: CommandType.Text)).ConfigureAwait(false);
        return rows.AsList();
    }

    #endregion

    private sealed class CRegionAgg
    {
        public int Id { get; set; }
        public string Region { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaUltimaModificacion { get; set; }
        public int TotalFiltrados { get; set; }
    }

    private sealed class CRutaAgg
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaUltimaModificacion { get; set; }
        public int TotalFiltrados { get; set; }
    }

    private sealed class CAlertaAgg
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int Severidad { get; set; }
        public bool ActivarAlerta { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaUltimaModificacion { get; set; }
        public int TotalFiltrados { get; set; }
    }

    private sealed class CStatusAgg
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaUltimaModificacion { get; set; }
        public int TotalFiltrados { get; set; }
    }

    private sealed class CDetalleAgg
    {
        public int Id { get; set; }
        public string NombreDetalle { get; set; } = string.Empty;
        public int TipoDato { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaUltimaModificacion { get; set; }
        public int TotalFiltrados { get; set; }
    }

    private sealed class CAtributoAgg
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int TipoDato { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaUltimaModificacion { get; set; }
        public int TotalFiltrados { get; set; }
    }

    private sealed class CDispositivoAgg
    {
        public int Id { get; set; }
        public string NombreDispositivo { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaUltimaModificacion { get; set; }
        public int TotalFiltrados { get; set; }
    }

    private sealed class CatalogLocationAgg
    {
        public int Id { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public int IdRegion { get; set; }
        public string RegionNombre { get; set; } = string.Empty;
        public string? Ubicacion { get; set; }
        public string? Estado { get; set; }
        public int TotalFiltrados { get; set; }
    }
}
