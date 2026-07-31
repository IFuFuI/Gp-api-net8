using System.Data;
using System.Globalization;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Campana;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ATT.Monitor.Api.Infrastructure.Persistence;

/// <summary>Paridad <c>CampanaData.cs</c> API v1.</summary>
public sealed class CampanaDataService(IConfiguration configuration) : ICampanaData
{
    private string ConnectionString =>
        configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:SqlServer.");

    public async Task<int> InsertCampanaAsync(
        string nombre,
        string tipo,
        DateTime fechaInicio,
        DateTime fechaTermino,
        int idArchivo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@NOMBRE", nombre);
            parameters.Add("@TIPO", tipo);
            parameters.Add("@FECHAINICIO", fechaInicio);
            parameters.Add("@FECHATERMINO", fechaTermino);
            parameters.Add("@ID_ARCHIVO", idArchivo);
            var idCampana = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    "dbo.SP_INSERT_CAMPANA",
                    parameters,
                    cancellationToken: cancellationToken,
                    commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
            return idCampana ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    public async Task<IEnumerable<CampanaDto>> GetCampanasAsync(CampanaRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@IGNORAR", request.Ignorar);
        parameters.Add("@CANTIDAD_FILA", request.CantidadFila);
        parameters.Add("@FILTRO", request.Filtro);
        parameters.Add("@ORDEN", request.Orden);
        parameters.Add("@DIR", request.Dir);
        parameters.Add("@FechaInicio", request.FechaInicio);
        parameters.Add("@FechaFin", request.FechaFin);

        var result = await connection.QueryAsync<CampanaDto>(
            new CommandDefinition(
                "dbo.SP_GET_CAMPANAS",
                parameters,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        var campanas = result.ToList();

        var hoy = DateTime.Today;
        foreach (var c in campanas)
        {
            if (DateTime.TryParse(c.FechaInicio, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaInicio) &&
                DateTime.TryParse(c.FechaTermino, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaTermino))
            {
                c.Estatus = fechaInicio <= hoy && hoy <= fechaTermino ? "ACTIVA" : "INACTIVA";
            }
            else
                c.Estatus = "INACTIVA";
        }

        return campanas;
    }

    public async Task<int> InsertArchivoCampanaAsync(string pathArchivo, string nombreArchivo, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@PATH_ARCHIVO", pathArchivo);
            parameters.Add("@NOMBRE_ARCHIVO", nombreArchivo);
            var idArchivo = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    "dbo.SP_INSERT_ARCHIVO_CAMPANA",
                    parameters,
                    cancellationToken: cancellationToken,
                    commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
            return idArchivo ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    public async Task<bool> InsertCampanaEpAsync(int idCampana, string ep, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(ConnectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@ID_CAMPANA", idCampana);
            parameters.Add("@EP", ep);
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "dbo.SP_Insert_R_CAMPANA_EP",
                    parameters,
                    cancellationToken: cancellationToken,
                    commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<ConsultaEpCampanaResponse>> GetCampanasEPAsync(
        CampanaEPRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@id_campana", request.ID_CAMPANA);
        const string query = """
            SELECT EP, Status, FechaFin
            FROM dbo.fn_consulta_ep_campana(@id_campana)
            """;
        return await connection.QueryAsync<ConsultaEpCampanaResponse>(
            new CommandDefinition(query, parameters, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<CampanaSpResultDto> BajaCampanaAsync(BajaCampanaRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@id_campana", request.ID_CAMPANA);
        parameters.Add("@USUARIO", "ADMIN");
        parameters.Add("@RESULTINT", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("@RESULTSTRING", dbType: DbType.String, size: 255, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.SP_BAJA_CAMPANIA",
                parameters,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);

        return new CampanaSpResultDto
        {
            ResultInt = parameters.Get<int>("@RESULTINT"),
            ResultString = parameters.Get<string>("@RESULTSTRING") ?? string.Empty
        };
    }

    public async Task<CampanaSpResultDto> ActualizarCampanaAsync(
        ActualizarCampanaRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var p = new DynamicParameters();
        p.Add("@idcampana", request.ID_CAMPANA);
        p.Add("@FechaInicio", request.FechaInicio);
        p.Add("@FechaFin", request.FechaFin);
        p.Add("@Resultado", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("@Mensaje", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.SP_UPDATE_FECHAS_CAMPANA",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);

        return new CampanaSpResultDto
        {
            ResultInt = p.Get<int>("@Resultado"),
            ResultString = p.Get<string>("@Mensaje") ?? string.Empty
        };
    }

    public async Task<string?> ObtenerRutaPorCampanaAsync(int idCampana, CancellationToken cancellationToken = default)
    {
        const string query = "SELECT dbo.fn_ObtenerRutaArchivoPorCampana(@IdCampana)";
        await using var connection = new SqlConnection(ConnectionString);
        return await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(query, new { IdCampana = idCampana }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<(int ResultInt, string ResultString)> ActualizarZipCampanaAsync(
        int idCampana,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await using var command = new SqlCommand("dbo.SP_ACTUALIZA_ZIP_INCAMPANIA", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@ID_CAMPANA", idCampana);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return (
                reader.GetInt32(reader.GetOrdinal("ResultInt")),
                reader.GetString(reader.GetOrdinal("ResultString")));
        }

        return (0, "Sin respuesta del SP");
    }

    public async Task<bool> EpExistsInCampanaAsync(int idCampana, string ep, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ep))
            return false;

        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.R_CAMPANA_EP
            WHERE ID_CAMPANA = @idCampana AND EP = @ep;
            """;
        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { idCampana, ep = ep.Trim() }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return count > 0;
    }

    public async Task<CampanaSpResultDto> EliminarEpCampanaAsync(
        EliminarEpCampanaRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@ID_CAMPANA", request.ID_CAMPANA);
        parameters.Add("@EP", request.EP.Trim());
        parameters.Add("@USUARIO", string.IsNullOrWhiteSpace(request.Usuario) ? "ADMIN" : request.Usuario.Trim());
        parameters.Add("@RESULTINT", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("@RESULTSTRING", dbType: DbType.String, size: 255, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.SP_Delete_EP_CAMPANIA",
                parameters,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);

        return new CampanaSpResultDto
        {
            ResultInt = parameters.Get<int>("@RESULTINT"),
            ResultString = parameters.Get<string>("@RESULTSTRING") ?? string.Empty
        };
    }
}
