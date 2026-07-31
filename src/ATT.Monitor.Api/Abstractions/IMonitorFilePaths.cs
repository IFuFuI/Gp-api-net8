namespace ATT.Monitor.Api.Abstractions;

/// <summary>Rutas de disco para paquetes / journal (config + variables de entorno).</summary>
public interface IMonitorFilePaths
{
    /// <summary>Ruta base ATM o cadena vacía si no está configurada.</summary>
    string GetArchivosAtmRoot();

    /// <summary>Ruta journal diario o cadena vacía.</summary>
    string GetJournalDiaRoot();
}
