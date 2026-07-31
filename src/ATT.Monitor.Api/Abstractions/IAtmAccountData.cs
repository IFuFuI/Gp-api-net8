using ATT.Monitor.Api.Models.Login;

namespace ATT.Monitor.Api.Abstractions;

/// <summary>Operaciones ATM usadas por login y permisos (paridad <c>IDataAtm</c> parcial API v1).</summary>
public interface IAtmAccountData
{
    Task<ProcedureResultDto> ValidatAtmAsync(ELoginAtmCajero atm, CancellationToken cancellationToken = default);
    Task<PermisosResponseDto> GetPermisosAsync(GrupoPermisosRequest request, CancellationToken cancellationToken = default);
    Task<ProcedureResultDto?> InsertBitacoraUsuariosAsync(BitacoraUsuariosRequest request, CancellationToken cancellationToken = default);
}
