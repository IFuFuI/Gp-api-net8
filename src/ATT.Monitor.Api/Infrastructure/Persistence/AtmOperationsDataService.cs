using System.Data;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Atm;
using ATT.Monitor.Api.Models.Login;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ATT.Monitor.Api.Infrastructure.Persistence;

/// <summary>Dapper: <c>SP_INSERT_COMANDO_ATM</c>, <c>SP_INSERT_ESTADO_ATM</c>, <c>SP_UPDATE_COMANDO</c>, <c>SP_INSERT_STATUS_HW_DISPOSITIVOS</c>, <c>SP_INSERT_VERSION_DSC_ATM</c>, <c>SP_INSERT_SISTEMAOPERA_EP</c>.</summary>
public sealed class AtmOperationsDataService(IConfiguration configuration) : IAtmOperationsData
{
    private string ConnectionString =>
        configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:SqlServer.");

    public async Task<ProcedureResultDto> InsertComandoAtmAsync(
        string idCajero,
        int idPaquete,
        string comando,
        string idUsuario,
        string? url = null,
        CancellationToken cancellationToken = default)
    {
        var (comandoStored, urlStored) = AtmComandoPathCodec.ApplyForStorage(comando, url);

        await using var connection = new SqlConnection(ConnectionString);
        var p = new DynamicParameters();
        p.Add("@ID_CAJERO", idCajero, DbType.String);
        p.Add("@ID_PAQUETE", idPaquete, DbType.Int32);
        p.Add("@COMANDO", comandoStored, DbType.String);
        p.Add("@ID_USUARIO", idUsuario, DbType.String);
        p.Add("@URL", urlStored, DbType.String);

        ProcedureResultDto? result;
        try
        {
            result = await connection.QueryFirstOrDefaultAsync<ProcedureResultDto>(
                new CommandDefinition(
                    "dbo.SP_INSERT_COMANDO_ATM",
                    p,
                    cancellationToken: cancellationToken,
                    commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        }
        catch (SqlException ex) when (ex.Number == 8145 && !string.IsNullOrWhiteSpace(urlStored))
        {
            p = new DynamicParameters();
            p.Add("@ID_CAJERO", idCajero, DbType.String);
            p.Add("@ID_PAQUETE", idPaquete, DbType.Int32);
            p.Add("@COMANDO", comandoStored, DbType.String);
            p.Add("@ID_USUARIO", idUsuario, DbType.String);

            result = await connection.QueryFirstOrDefaultAsync<ProcedureResultDto>(
                new CommandDefinition(
                    "dbo.SP_INSERT_COMANDO_ATM",
                    p,
                    cancellationToken: cancellationToken,
                    commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        }

        return result ?? new ProcedureResultDto { ResultInt = 0, ResultString = "ERROR" };
    }

    public async Task<string?> PostEstadoAsync(EstadoAtmRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var p = new DynamicParameters();
        p.Add("@ID_CAJERO", request.IdCajero);
        p.Add("@ESTADO", request.Estado);

        return await connection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(
                "dbo.SP_INSERT_ESTADO_ATM",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }

    public async Task<ProcedureResultDto?> UpdateComandoAsync(EIdSolComando request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var p = new DynamicParameters();
        p.Add("@ID_SOLICITUD", request.idSolicitud, DbType.Int32);
        p.Add("@STATUS", request.status, DbType.String);
        p.Add("@ResultInt", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("@ResultString", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.SP_UPDATE_COMANDO",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);

        return new ProcedureResultDto
        {
            ResultInt = p.Get<int?>("@ResultInt"),
            ResultString = p.Get<string>("@ResultString") ?? string.Empty
        };
    }

    public async Task<ProcedureResultDto?> InsertStatusHwDispositivosAsync(
        CajeroAlarma request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var p = new DynamicParameters();
        p.Add("@ID_CAJERO", request.IdCajero, DbType.String, ParameterDirection.Input);
        p.Add("@DISPOSITIVO", request.Dispositivo, DbType.Int32, ParameterDirection.Input);
        p.Add("@ESTADO", request.Estado, DbType.Int32, ParameterDirection.Input);
        p.Add("@ALARMA", request.Alarma, DbType.Int32, ParameterDirection.Input);
        p.Add("@ResultInt", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("@ResultString", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.SP_INSERT_STATUS_HW_DISPOSITIVOS",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);

        return new ProcedureResultDto
        {
            ResultInt = p.Get<int?>("@ResultInt"),
            ResultString = p.Get<string>("@ResultString") ?? string.Empty
        };
    }

    public async Task<ProcedureResultDto?> InsertVersionAsync(EVersionAtm request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var p = new DynamicParameters();
        p.Add("@ID_CAJERO", request.ID_ATM, DbType.String, ParameterDirection.Input);
        p.Add("@VERSION_DSC", request.VERSION, DbType.String, ParameterDirection.Input);
        p.Add("@ResultInt", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("@ResultString", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            new CommandDefinition(
                "dbo.SP_INSERT_VERSION_DSC_ATM",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);

        return new ProcedureResultDto
        {
            ResultInt = p.Get<int?>("@ResultInt"),
            ResultString = p.Get<string>("@ResultString") ?? string.Empty
        };
    }

    public async Task<ProcedureResultDto?> InsertSistemaInfoAsync(SistemaInfo sistema, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var p = new DynamicParameters();
        p.Add("@SistemaOperativo", sistema.SistemaOperativo, DbType.String);
        p.Add("@MemoriaRamGB", sistema.MemoriaRamGB, DbType.Decimal);
        p.Add("@Idioma", sistema.Idioma, DbType.String);
        p.Add("@Resolucion", sistema.Resolucion, DbType.String);
        p.Add("@Procesador", sistema.Procesador, DbType.String);
        p.Add("@ZonaHoraria", sistema.ZonaHoraria, DbType.String);
        p.Add("@NombreEquipo", sistema.NombreEquipo, DbType.String);
        p.Add("@EP", sistema.EP, DbType.String);
        p.Add("@AlmacenamientoGB", sistema.AlmacenamientoGB, DbType.Decimal);

        var result = await connection.QueryFirstOrDefaultAsync<ProcedureResultDto>(
            new CommandDefinition(
                "dbo.SP_INSERT_SISTEMAOPERA_EP",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);

        return result ?? new ProcedureResultDto { ResultInt = 0, ResultString = "No se obtuvo respuesta del SP" };
    }
}
