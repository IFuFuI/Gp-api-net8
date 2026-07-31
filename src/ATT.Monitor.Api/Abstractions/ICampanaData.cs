using ATT.Monitor.Api.Models.Campana;

namespace ATT.Monitor.Api.Abstractions;

/// <summary>Paridad <c>ICampanaData</c> / <c>CampanaData.cs</c> API v1.</summary>
public interface ICampanaData
{
    Task<int> InsertCampanaAsync(
        string nombre,
        string tipo,
        DateTime fechaInicio,
        DateTime fechaTermino,
        int idArchivo,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<CampanaDto>> GetCampanasAsync(CampanaRequest request, CancellationToken cancellationToken = default);

    Task<int> InsertArchivoCampanaAsync(string pathArchivo, string nombreArchivo, CancellationToken cancellationToken = default);

    Task<bool> InsertCampanaEpAsync(int idCampana, string ep, CancellationToken cancellationToken = default);

    Task<IEnumerable<ConsultaEpCampanaResponse>> GetCampanasEPAsync(CampanaEPRequest request, CancellationToken cancellationToken = default);

    Task<CampanaSpResultDto> BajaCampanaAsync(BajaCampanaRequest request, CancellationToken cancellationToken = default);

    Task<CampanaSpResultDto> ActualizarCampanaAsync(ActualizarCampanaRequest request, CancellationToken cancellationToken = default);

    Task<string?> ObtenerRutaPorCampanaAsync(int idCampana, CancellationToken cancellationToken = default);

    Task<(int ResultInt, string ResultString)> ActualizarZipCampanaAsync(int idCampana, CancellationToken cancellationToken = default);

    Task<bool> EpExistsInCampanaAsync(int idCampana, string ep, CancellationToken cancellationToken = default);

    Task<CampanaSpResultDto> EliminarEpCampanaAsync(EliminarEpCampanaRequest request, CancellationToken cancellationToken = default);
}
