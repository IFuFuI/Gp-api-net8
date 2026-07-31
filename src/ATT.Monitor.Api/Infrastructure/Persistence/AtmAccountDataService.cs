using System.Data;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Login;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ATT.Monitor.Api.Infrastructure.Persistence;

/// <summary>
/// Dapper contra <c>dbo.FUNC_VALIDA_ATM</c>, <c>FUNC_GET_PERMISOS</c>, <c>SP_INSERT_BITACORA_USUARIOS</c>.
/// </summary>
public sealed class AtmAccountDataService(IConfiguration configuration) : IAtmAccountData
{
    private const string ErrorMessage = "ERROR";

    private string ConnectionString =>
        configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:SqlServer.");

    public async Task<ProcedureResultDto> ValidatAtmAsync(ELoginAtmCajero atm, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sql = """
            SELECT ResultInt, ResultString
            FROM dbo.FUNC_VALIDA_ATM(@ID_ATM, @MODELO, @IP, @SOCKET);
            """;
        var result = await connection.QueryFirstOrDefaultAsync<ProcedureResultDto>(
            new CommandDefinition(
                sql,
                new { ID_ATM = atm.ID_ATM, MODELO = atm.MODELO, IP = atm.IP, SOCKET = atm.SOCKET },
                cancellationToken: cancellationToken,
                commandType: CommandType.Text)).ConfigureAwait(false);

        return result ?? new ProcedureResultDto { ResultInt = 0, ResultString = ErrorMessage };
    }

    public async Task<PermisosResponseDto> GetPermisosAsync(GrupoPermisosRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        const string sqlMenu = """
            SELECT *
            FROM dbo.FUNC_GET_PERMISOS(@ID_GRUPO);
            """;
        const string sqlSub = """
            SELECT *
            FROM dbo.FUNC_GET_SUBPERMISOS(@ID_GRUPO);
            """;
        var param = new { ID_GRUPO = request.idgrupo };
        var menu = await connection
            .QueryAsync<MenuPermisoDto>(new CommandDefinition(sqlMenu, param, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        var submenu = await connection
            .QueryAsync<SubmenuPermisoDto>(new CommandDefinition(sqlSub, param, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return new PermisosResponseDto
        {
            Menu = menu.ToList(),
            Submenu = submenu.ToList()
        };
    }

    public async Task<ProcedureResultDto?> InsertBitacoraUsuariosAsync(
        BitacoraUsuariosRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var p = new DynamicParameters();
        p.Add("@ATTUID", request.ATTUID, DbType.String);
        p.Add("@EMAIL", request.EMAIL, DbType.String);
        p.Add("@GIVEN_NAME", request.GIVEN_NAME, DbType.String);
        p.Add("@SURNAME", request.SURNAME, DbType.String);
        p.Add("@NAME_IDENTIFIER", request.NAME_IDENTIFIER, DbType.String);
        p.Add("@GROUPS", request.GROUPS, DbType.String);
        p.Add("@AUTHENTICATION_INSTANT", request.AUTHENTICATION_INSTANT, DbType.DateTime2);
        p.Add("@TENANTID", request.TENANTID, DbType.String);

        return await connection.QueryFirstOrDefaultAsync<ProcedureResultDto>(
            new CommandDefinition(
                "dbo.SP_INSERT_BITACORA_USUARIOS",
                p,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
    }
}
