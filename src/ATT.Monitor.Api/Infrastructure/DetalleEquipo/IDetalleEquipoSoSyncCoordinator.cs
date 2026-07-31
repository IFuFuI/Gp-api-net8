namespace ATT.Monitor.Api.Infrastructure.DetalleEquipo;

public enum DetalleEquipoSoSyncRegisterKind
{
    NewlyQueued,
    AlreadyQueued,
    AlreadyCompleted
}

public sealed class DetalleEquipoSoSyncRegisterResult
{
    public DetalleEquipoSoSyncRegisterKind Kind { get; init; }
}

public enum DetalleEquipoSoSyncUiState
{
    Unknown,
    Pending,
    InFlight,
    Completed
}

public sealed class DetalleEquipoSoSyncStatusDto
{
    public DetalleEquipoSoSyncUiState State { get; init; }
}

public readonly record struct DetalleEquipoSoSyncClaimed(Guid IdSesion, string IdAtm);

/// <summary>
/// Cola en memoria: una sincronización SO/DD por sesión de detalle. Sin tabla intermedia en BD.
/// Para farm de API, sustituir por Redis o tabla dedicada.
/// </summary>
public interface IDetalleEquipoSoSyncCoordinator
{
    DetalleEquipoSoSyncRegisterResult RegisterRequest(string idAtm, Guid idSesion);

    DetalleEquipoSoSyncStatusDto GetStatus(string idAtm, Guid idSesion);

    /// <summary>El agente toma la solicitud más antigua pendiente para el cajero.</summary>
    DetalleEquipoSoSyncClaimed? TryClaimNext(string idAtm);

    void MarkSubmitSuccess(string idAtm, Guid idSesion);

    /// <summary>Revierte a pendiente para reintento del agente.</summary>
    void MarkSubmitFailure(string idAtm, Guid idSesion);

    /// <summary>Elimina estado al cerrar detalle (libera memoria, cancela trabajo pendiente).</summary>
    void AbandonSession(Guid idSesion);
}
