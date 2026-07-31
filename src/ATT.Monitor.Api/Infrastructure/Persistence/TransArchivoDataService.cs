using System.Data;
using System.IO;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Login;
using ATT.Monitor.Api.Models.TransArchivo;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ATT.Monitor.Api.Infrastructure.Persistence;

/// <summary>Dapper + rutas para paquetes / ZIP / catálogo (paridad <c>DataTransArchivo</c>).</summary>
public sealed class TransArchivoDataService(IConfiguration configuration, IMonitorFilePaths filePaths) : ITransArchivoData
{
    private string ConnectionString =>
        configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:SqlServer.");

    public async Task<string?> GetCatalogoArchivoAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT dbo.FUNC_GET_CATALOGO_ARCHIVO()";
        await using var connection = new SqlConnection(ConnectionString);
        return await connection.ExecuteScalarAsync<string>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IEnumerable<BitacoraComandoDto>> GetBitacoraComandosAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT *
            FROM dbo.FUNC_GET_BITACORA_COMANDOS()
            ORDER BY FECHA_ALTA DESC;
            """;
        await using var connection = new SqlConnection(ConnectionString);
        return await connection.QueryAsync<BitacoraComandoDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<string?> GetLocationsJsonRawAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT dbo.FUNC_GETLOCATIONJSON()";
        await using var connection = new SqlConnection(ConnectionString);
        return await connection.ExecuteScalarAsync<string>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<string?> GetDeviceConfigJsonAsync(int idLocation, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT dbo.FUNC_GETDEVICECONFIG_JSON(@Id_Location)";
        await using var connection = new SqlConnection(ConnectionString);
        return await connection.ExecuteScalarAsync<string>(
            new CommandDefinition(sql, new { Id_Location = idLocation }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<MArchivo?> GetPaqueteAsync(string idAtm, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var p = new DynamicParameters();
        p.Add("@ID_CAJERO", idAtm);
        return await connection.QueryFirstOrDefaultAsync<MArchivo>(
            new CommandDefinition(
                "dbo.SP_GET_PAQUETES",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    public async Task<ProcedureResultDto> InsertPaqueteAsync(
        string idatm,
        string nombre,
        string tipoarchivo,
        CancellationToken cancellationToken = default)
    {
        var root = filePaths.GetArchivosAtmRoot();
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException(
                "Ruta de archivos EP no configurada: FileStorage:ArchivosAtmPath o variable ARCHIVOS_ATM_PATH.");
        }

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var parametros = new DynamicParameters();
        parametros.Add("@ID_CAJERO", idatm);
        parametros.Add("@NOMBRE", tipoarchivo);
        parametros.Add("@RUTA", Path.Combine(root, nombre));

        var res = await connection.QuerySingleAsync<ProcedureResultDto>(
            new CommandDefinition(
                "dbo.SP_INSERT_PAQUETE",
                parametros,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);

        var idPaquete = res.ResultInt ?? 0;
        await InsertComandoAsync(idatm, idPaquete, string.Empty, cancellationToken).ConfigureAwait(false);

        return res;
    }

    public async Task<ProcedureResultDto> InsertComandoAsync(
        string idatm,
        int idPaquete,
        string comando,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var parametros = new DynamicParameters();
        parametros.Add("@ID_CAJERO", idatm);
        parametros.Add("@ID_PAQUETE", idPaquete);
        parametros.Add("@COMANDO", comando);
        return await connection.QuerySingleAsync<ProcedureResultDto>(
            new CommandDefinition(
                "dbo.SP_INSERT_COMANDO_ATM",
                parametros,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    public async Task<int> InsertArchivoZipAsync(ArchivoZipDto archivo, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var p = new DynamicParameters();
        p.Add("@NOMBRE_ARCHIVO_IN", archivo.NombreArchivo);
        p.Add("@PATH_SHARE_IN", archivo.Path);
        p.Add("@TIPO", archivo.Tipo);
        p.Add("@ID_ATM", (object?)archivo.IdAtm ?? DBNull.Value);
        p.Add("@NOMBRE_ZIP", (object?)archivo.NombreZip ?? DBNull.Value);
        p.Add("@APLICACION", archivo.Aplicacion);

        var scalar = await connection.ExecuteScalarAsync<object>(
            new CommandDefinition(
                "dbo.SP_INSERT_ARCHIVO_ZIP",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);

        return scalar is null or DBNull ? 0 : Convert.ToInt32(scalar, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<InfoZipDto?> GetInfoZipAsync(EidAtm request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT NOMBRE_ZIP, FECHA_ALTA, Nombre
            FROM dbo.FUNC_GET_INFO_ZIP(@ID_ATM, @ID_ARCHIVO, @APP);
            """;
        await using var connection = new SqlConnection(ConnectionString);
        return await connection.QueryFirstOrDefaultAsync<InfoZipDto>(
            new CommandDefinition(
                sql,
                new { ID_ATM = request.IDATM, ID_ARCHIVO = request.ID_TIPO, APP = request.APLICACION },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task GuardarArchivosExtraidosAsync(
        IEnumerable<string> archivos,
        string carpetaBase,
        string nombreZip,
        int tipo,
        string idAtm,
        int aplicacion,
        CancellationToken cancellationToken = default)
    {
        foreach (var archivo in archivos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = new FileInfo(archivo);
            var dto = new ArchivoZipDto
            {
                NombreArchivo = info.Name,
                Path = archivo.Replace(carpetaBase, string.Empty, StringComparison.OrdinalIgnoreCase),
                Tipo = tipo,
                IdAtm = idAtm,
                NombreZip = nombreZip,
                Aplicacion = aplicacion
            };
            await InsertArchivoZipAsync(dto, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<ProcedureResultDto?> InsertArchivoComandoAtmAsync(
        int? idSolicitud,
        string nombreDoc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parametros = new DynamicParameters();
        parametros.Add("@ID_SOLICITUD", idSolicitud, DbType.Int32, ParameterDirection.Input);
        parametros.Add("@NOMBRE_ARCHIVO", nombreDoc, DbType.String, ParameterDirection.Input);
        parametros.Add("@ResultInt", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parametros.Add("@ResultString", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.SP_UPDATE_NOMBRE_FILE_CMD",
                parametros,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);

        return new ProcedureResultDto
        {
            ResultInt = parametros.Get<int?>("@ResultInt"),
            ResultString = parametros.Get<string>("@ResultString") ?? string.Empty
        };
    }
}
