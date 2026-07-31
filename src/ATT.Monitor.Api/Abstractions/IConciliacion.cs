using ATT.Monitor.Api.Models.Conciliacion;
namespace ATT.Monitor.Api.Abstractions;

public interface IConciliacion
{
    Task<string> GetDetalleConciliacionAsync(IdCargaConciliacionResponse post,
             CancellationToken cancellationToken = default);
    Task<string> GetConciliacionAsync(ConciliacionResumenTransaccionalRequest post, 
                CancellationToken cancellationToken = default);

    Task<(int IdCarga, bool EsReproceso, int? IdCargaOrigen)> RegistrarCargaAsync(
        string nombreArchivo,
        string hashArchivo,
        string usuarioCarga,
        CancellationToken cancellationToken = default);

    Task InsertarDetalleAsync(
        int idCarga,
        IEnumerable<AutopagoDetalleRow> detalle,
        CancellationToken cancellationToken = default);
}