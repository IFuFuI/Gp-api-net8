using System.Data;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Administrador;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ATT.Monitor.Api.Infrastructure.Persistence;

/// <summary>Paridad <c>Data/Administrador.cs</c> API v1.</summary>
public sealed class AdministradorDataService(IConfiguration configuration) : IAdministradorData
{
    private string ConnectionString =>
        configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:SqlServer.");

    public async Task<IEnumerable<EstatusAceptadorBilletes>> GetStatusBilletesAceptadorAsync(
        int pageNumber, int pageSize, string? buscar, string? orderBy, string? orderDir,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@PageNumber", pageNumber);
        parameters.Add("@PageSize", pageSize);
        parameters.Add("@Buscar", buscar);
        parameters.Add("@OrderBy", orderBy ?? "ID");
        parameters.Add("@OrderDir", orderDir ?? "ASC");

        const string sql = """
            SELECT *
            FROM dbo.FN_CONSULTA_C_ESTATUS_ACEPTADOR_BILLETES
            (@PageNumber, @PageSize, @Buscar, @OrderBy, @OrderDir)
            """;

        return await connection.QueryAsync<EstatusAceptadorBilletes>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken, commandType: CommandType.Text)).ConfigureAwait(false);
    }

    public async Task<int> ActualizarStatusBilletesAceptadorAsync(
        int id, string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@ID", id);
        parameters.Add("@DESCRIPCION", descripcion);
        parameters.Add("@SEVERIDAD", severidad);
        parameters.Add("@ACTIVAR_ALERTA", activarAlerta);

        var scalar = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "SP_ACTUALIZA_C_ESTATUS_ACEPTADOR_BILLETES",
                parameters,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        return scalar ?? 0;
    }

    public async Task<int> CrearStatusBilletesAceptadorAsync(
        string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@DESCRIPCION", descripcion);
        parameters.Add("@SEVERIDAD", severidad);
        parameters.Add("@ACTIVAR_ALERTA", activarAlerta);

        var scalar = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "SP_ALTA_C_ESTATUS_ACEPTADOR_BILLETES",
                parameters,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        return scalar ?? 0;
    }

    public async Task<IEnumerable<EstatusAceptadorMonedas>> GetStatusMonedasAceptadorAsync(
        int pageNumber, int pageSize, string? buscar, string? orderBy, string? orderDir,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@PAGE_NUMBER", pageNumber);
        parameters.Add("@PAGE_SIZE", pageSize);
        parameters.Add("@BUSCAR", buscar);
        parameters.Add("@ORDER_BY", orderBy ?? "ID");
        parameters.Add("@ORDER_DIR", orderDir ?? "ASC");

        const string sql = """
            SELECT *
            FROM dbo.FN_CONSULTA_C_ESTATUS_ACEPTADOR_MONEDAS
            (@PAGE_NUMBER, @PAGE_SIZE, @BUSCAR, @ORDER_BY, @ORDER_DIR)
            """;

        return await connection.QueryAsync<EstatusAceptadorMonedas>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken, commandType: CommandType.Text)).ConfigureAwait(false);
    }

    public async Task<int> ActualizarStatusMonedasAceptadorAsync(
        int id, string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@ID", id);
        parameters.Add("@DESCRIPCION", descripcion);
        parameters.Add("@SEVERIDAD", severidad);
        parameters.Add("@ACTIVAR_ALERTA", activarAlerta);

        var scalar = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "SP_ACTUALIZA_C_ESTATUS_ACEPTADOR_MONEDAS",
                parameters,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        return scalar ?? 0;
    }

    public async Task<int> CrearStatusMonedasAceptadorAsync(
        string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@DESCRIPCION", descripcion);
        parameters.Add("@SEVERIDAD", severidad);
        parameters.Add("@ACTIVAR_ALERTA", activarAlerta);

        var scalar = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "SP_ALTA_C_ESTATUS_ACEPTADOR_MONEDAS",
                parameters,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        return scalar ?? 0;
    }

    public async Task<IEnumerable<EstatusDispensadorBilletes>> GetStatusBilletesDispensadorAsync(
        int pageNumber, int pageSize, string? buscar, string? orderBy, string? orderDir,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@PAGE_NUMBER", pageNumber);
        parameters.Add("@PAGE_SIZE", pageSize);
        parameters.Add("@BUSCAR", buscar);
        parameters.Add("@ORDER_BY", orderBy ?? "ID");
        parameters.Add("@ORDER_DIR", orderDir ?? "ASC");

        const string sql = """
            SELECT *
            FROM dbo.FN_CONSULTA_C_ESTATUS_DISPENSADOR_BILLETES
            (@PAGE_NUMBER, @PAGE_SIZE, @BUSCAR, @ORDER_BY, @ORDER_DIR)
            """;

        return await connection.QueryAsync<EstatusDispensadorBilletes>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken, commandType: CommandType.Text)).ConfigureAwait(false);
    }

    public async Task<int> ActualizarStatusBilletesDispensadorAsync(
        int id, string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@ID", id);
        parameters.Add("@DESCRIPCION", descripcion);
        parameters.Add("@SEVERIDAD", severidad);
        parameters.Add("@ACTIVAR_ALERTA", activarAlerta);

        var scalar = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "SP_ACTUALIZA_C_ESTATUS_DISPENSADOR_BILLETES",
                parameters,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        return scalar ?? 0;
    }

    public async Task<int> CrearStatusBilletesDispensadorAsync(
        string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@DESCRIPCION", descripcion);
        parameters.Add("@SEVERIDAD", severidad);
        parameters.Add("@ACTIVAR_ALERTA", activarAlerta);

        var scalar = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "SP_ALTA_C_ESTATUS_DISPENSADOR_BILLETES",
                parameters,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        return scalar ?? 0;
    }

    public async Task<IEnumerable<EstatusDispensadorMonedas>> GetStatusMonedasDispensadorAsync(
        int pageNumber, int pageSize, string? buscar, string? orderBy, string? orderDir,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@PAGE_NUMBER", pageNumber);
        parameters.Add("@PAGE_SIZE", pageSize);
        parameters.Add("@BUSCAR", buscar);
        parameters.Add("@ORDER_BY", orderBy ?? "ID");
        parameters.Add("@ORDER_DIR", orderDir ?? "ASC");

        const string sql = """
            SELECT *
            FROM dbo.FN_CONSULTA_C_ESTATUS_DISPENSADOR_MONEDAS
            (@PAGE_NUMBER, @PAGE_SIZE, @BUSCAR, @ORDER_BY, @ORDER_DIR)
            """;

        return await connection.QueryAsync<EstatusDispensadorMonedas>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken, commandType: CommandType.Text)).ConfigureAwait(false);
    }

    public async Task<int> ActualizarStatusMonedasDispensadorAsync(
        int id, string nombre, int severidad,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@ID", id);
        parameters.Add("@NOMBRE", nombre);
        parameters.Add("@SEVERIDAD", severidad);

        var scalar = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "SP_ACTUALIZA_C_ESTATUS_DISPENSADOR_MONEDAS",
                parameters,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        return scalar ?? 0;
    }

    public async Task<int> CrearStatusMonedasDispensadorAsync(
        string nombre, int severidad,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@NOMBRE", nombre);
        parameters.Add("@SEVERIDAD", severidad);

        var scalar = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "SP_ALTA_C_ESTATUS_DISPENSADOR_MONEDAS",
                parameters,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        return scalar ?? 0;
    }

    public async Task<IEnumerable<EstatusImpresora>> GetStatusImpresoraAsync(
        int pageNumber, int pageSize, string? buscar, string? orderBy, string? orderDir,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@PAGE_NUMBER", pageNumber);
        parameters.Add("@PAGE_SIZE", pageSize);
        parameters.Add("@BUSCAR", buscar);
        parameters.Add("@ORDER_BY", orderBy ?? "ID");
        parameters.Add("@ORDER_DIR", orderDir ?? "ASC");

        const string sql = """
            SELECT *
            FROM dbo.FN_CONSULTA_C_ESTATUS_IMPRESORA
            (@PAGE_NUMBER, @PAGE_SIZE, @BUSCAR, @ORDER_BY, @ORDER_DIR)
            """;

        return await connection.QueryAsync<EstatusImpresora>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken, commandType: CommandType.Text)).ConfigureAwait(false);
    }

    public async Task<int> ActualizarStatusImpresoraAsync(
        int id, string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@ID", id);
        parameters.Add("@DESCRIPCION", descripcion);
        parameters.Add("@SEVERIDAD", severidad);
        parameters.Add("@ACTIVAR_ALERTA", activarAlerta);

        var scalar = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "SP_ACTUALIZA_C_ESTATUS_IMPRESORA",
                parameters,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        return scalar ?? 0;
    }

    public async Task<int> CrearStatusImpresoraAsync(
        string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@DESCRIPCION", descripcion);
        parameters.Add("@SEVERIDAD", severidad);
        parameters.Add("@ACTIVAR_ALERTA", activarAlerta);

        var scalar = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "SP_ALTA_C_ESTATUS_IMPRESORA",
                parameters,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        return scalar ?? 0;
    }

    public async Task<IEnumerable<EstatusLectorCodigoBarras>> GetStatusLectorCodigoBarrasAsync(
        int pageNumber, int pageSize, string? buscar, string? orderBy, string? orderDir,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@PAGE_NUMBER", pageNumber);
        parameters.Add("@PAGE_SIZE", pageSize);
        parameters.Add("@BUSCAR", buscar);
        parameters.Add("@ORDER_BY", orderBy ?? "ID");
        parameters.Add("@ORDER_DIR", orderDir ?? "ASC");

        const string sql = """
            SELECT *
            FROM dbo.FN_CONSULTA_C_ESTATUS_LECTOR_CODIGO_BARRAS
            (@PAGE_NUMBER, @PAGE_SIZE, @BUSCAR, @ORDER_BY, @ORDER_DIR)
            """;

        return await connection.QueryAsync<EstatusLectorCodigoBarras>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken, commandType: CommandType.Text)).ConfigureAwait(false);
    }

    public async Task<int> ActualizarStatusLectorCodigoBarrasAsync(
        int id, string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@ID", id);
        parameters.Add("@DESCRIPCION", descripcion);
        parameters.Add("@SEVERIDAD", severidad);
        parameters.Add("@ACTIVAR_ALERTA", activarAlerta);

        var scalar = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "SP_ACTUALIZA_C_ESTATUS_LECTOR_CODIGO_BARRAS",
                parameters,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        return scalar ?? 0;
    }

    public async Task<int> CrearStatusLectorCodigoBarrasAsync(
        string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@DESCRIPCION", descripcion);
        parameters.Add("@SEVERIDAD", severidad);
        parameters.Add("@ACTIVAR_ALERTA", activarAlerta);

        var scalar = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                "SP_ALTA_C_ESTATUS_LECTOR_CODIGO_BARRAS",
                parameters,
                cancellationToken: cancellationToken,
                commandType: CommandType.StoredProcedure)).ConfigureAwait(false);
        return scalar ?? 0;
    }
}
