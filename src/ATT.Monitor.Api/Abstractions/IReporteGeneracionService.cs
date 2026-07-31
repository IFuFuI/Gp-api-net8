using ATT.Monitor.Api.Models.Dashboard;

namespace ATT.Monitor.Api.Abstractions;

public interface IReporteGeneracionService
{
    Task<GenerarReporteResponse> GenerarAsync(
        GenerarReporteRequest request,
        string usuario,
        CancellationToken cancellationToken = default);
}
