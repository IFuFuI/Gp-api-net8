using ATT.Monitor.Api.Models.Login;
using ATT.Monitor.Api.Models.TransArchivo;

namespace ATT.Monitor.Api.Abstractions;

/// <summary>Paridad <c>IDataTransArchivo</c> / <c>DataTransArchivo.cs</c> API v1.</summary>
public interface ITransArchivoData
{
    Task<string?> GetCatalogoArchivoAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<BitacoraComandoDto>> GetBitacoraComandosAsync(CancellationToken cancellationToken = default);
    Task<string?> GetLocationsJsonRawAsync(CancellationToken cancellationToken = default);
    Task<string?> GetDeviceConfigJsonAsync(int idLocation, CancellationToken cancellationToken = default);
    Task<MArchivo?> GetPaqueteAsync(string idAtm, CancellationToken cancellationToken = default);
    Task<ProcedureResultDto> InsertPaqueteAsync(string idatm, string nombre, string tipoarchivo, CancellationToken cancellationToken = default);
    Task<ProcedureResultDto> InsertComandoAsync(string idatm, int idPaquete, string comando, CancellationToken cancellationToken = default);
    Task<int> InsertArchivoZipAsync(ArchivoZipDto archivo, CancellationToken cancellationToken = default);
    Task<InfoZipDto?> GetInfoZipAsync(EidAtm request, CancellationToken cancellationToken = default);
    Task GuardarArchivosExtraidosAsync(
        IEnumerable<string> archivos,
        string carpetaBase,
        string nombreZip,
        int tipo,
        string idAtm,
        int aplicacion,
        CancellationToken cancellationToken = default);
    Task<ProcedureResultDto?> InsertArchivoComandoAtmAsync(int? idSolicitud, string nombreDoc, CancellationToken cancellationToken = default);
}
