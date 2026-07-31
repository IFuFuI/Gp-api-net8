using ATT.Monitor.Api.Models.Administrador;

namespace ATT.Monitor.Api.Abstractions;

/// <summary>Paridad <c>Administrador.cs</c> API v1 (catálogos FN / altas SP).</summary>
public interface IAdministradorData
{
    Task<IEnumerable<EstatusAceptadorBilletes>> GetStatusBilletesAceptadorAsync(
        int pageNumber, int pageSize, string? buscar, string? orderBy, string? orderDir,
        CancellationToken cancellationToken = default);

    Task<int> ActualizarStatusBilletesAceptadorAsync(
        int id, string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default);

    Task<int> CrearStatusBilletesAceptadorAsync(
        string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<EstatusAceptadorMonedas>> GetStatusMonedasAceptadorAsync(
        int pageNumber, int pageSize, string? buscar, string? orderBy, string? orderDir,
        CancellationToken cancellationToken = default);

    Task<int> ActualizarStatusMonedasAceptadorAsync(
        int id, string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default);

    Task<int> CrearStatusMonedasAceptadorAsync(
        string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<EstatusDispensadorBilletes>> GetStatusBilletesDispensadorAsync(
        int pageNumber, int pageSize, string? buscar, string? orderBy, string? orderDir,
        CancellationToken cancellationToken = default);

    Task<int> ActualizarStatusBilletesDispensadorAsync(
        int id, string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default);

    Task<int> CrearStatusBilletesDispensadorAsync(
        string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<EstatusDispensadorMonedas>> GetStatusMonedasDispensadorAsync(
        int pageNumber, int pageSize, string? buscar, string? orderBy, string? orderDir,
        CancellationToken cancellationToken = default);

    Task<int> ActualizarStatusMonedasDispensadorAsync(
        int id, string nombre, int severidad,
        CancellationToken cancellationToken = default);

    Task<int> CrearStatusMonedasDispensadorAsync(
        string nombre, int severidad,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<EstatusImpresora>> GetStatusImpresoraAsync(
        int pageNumber, int pageSize, string? buscar, string? orderBy, string? orderDir,
        CancellationToken cancellationToken = default);

    Task<int> ActualizarStatusImpresoraAsync(
        int id, string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default);

    Task<int> CrearStatusImpresoraAsync(
        string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<EstatusLectorCodigoBarras>> GetStatusLectorCodigoBarrasAsync(
        int pageNumber, int pageSize, string? buscar, string? orderBy, string? orderDir,
        CancellationToken cancellationToken = default);

    Task<int> ActualizarStatusLectorCodigoBarrasAsync(
        int id, string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default);

    Task<int> CrearStatusLectorCodigoBarrasAsync(
        string descripcion, int severidad, int activarAlerta,
        CancellationToken cancellationToken = default);
}
