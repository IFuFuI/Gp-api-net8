using ATT.Monitor.Api.Models.Atm;
using ATT.Monitor.Api.Models.Login;

namespace ATT.Monitor.Api.Abstractions;

/// <summary>Operaciones <c>DataATM</c> no cubiertas por <see cref="IAtmAccountData"/> (comandos, estado, versión, HW, sistema).</summary>
public interface IAtmOperationsData
{
    Task<ProcedureResultDto> InsertComandoAtmAsync(
        string idCajero,
        int idPaquete,
        string comando,
        string idUsuario,
        string? url = null,
        CancellationToken cancellationToken = default);

    Task<string?> PostEstadoAsync(EstadoAtmRequest request, CancellationToken cancellationToken = default);

    Task<ProcedureResultDto?> UpdateComandoAsync(EIdSolComando request, CancellationToken cancellationToken = default);

    Task<ProcedureResultDto?> InsertStatusHwDispositivosAsync(CajeroAlarma request, CancellationToken cancellationToken = default);

    Task<ProcedureResultDto?> InsertVersionAsync(EVersionAtm request, CancellationToken cancellationToken = default);

    Task<ProcedureResultDto?> InsertSistemaInfoAsync(SistemaInfo sistema, CancellationToken cancellationToken = default);
}
