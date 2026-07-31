using ATT.Monitor.Api.Models.Dashboard;

namespace ATT.Monitor.Api.Infrastructure.Persistence;

internal sealed class AgenteComunicacionDbRow
{
    public string? ID_ATM { get; set; }
    public string? KEEP_ALIVE { get; set; }
    public string? STATUS { get; set; }
    public DateTime? FECHA_ULTIMA_MODIFICACION { get; set; }
}

internal static class AgenteComunicacionEvaluator
{
    public static AgenteComunicacionEpResponse Evaluate(AgenteComunicacionDbRow? row, int thresholdMinutes)
    {
        if (row?.FECHA_ULTIMA_MODIFICACION is null)
        {
            return new AgenteComunicacionEpResponse
            {
                IdAtm = row?.ID_ATM,
                KeepAlive = row?.KEEP_ALIVE,
                Status = row?.STATUS,
                EnLinea = false,
                Detalle = "Sin registro de keep-alive del agente."
            };
        }

        var ultimo = row.FECHA_ULTIMA_MODIFICACION.Value;
        if (ultimo.Kind == DateTimeKind.Unspecified)
            ultimo = DateTime.SpecifyKind(ultimo, DateTimeKind.Local);

        var age = DateTime.Now - ultimo;
        var status = (row.STATUS ?? string.Empty).Trim();
        var keep = (row.KEEP_ALIVE ?? string.Empty).Trim();
        var statusUpper = status.ToUpperInvariant();
        var statusError = statusUpper.Contains("OFFLINE", StringComparison.Ordinal)
                          || statusUpper.Contains("ERROR", StringComparison.Ordinal)
                          || statusUpper.Contains("FALLA", StringComparison.Ordinal)
                          || statusUpper.Contains("DOWN", StringComparison.Ordinal);

        var enLinea = age.TotalMinutes <= thresholdMinutes && !statusError;

        var detalle = enLinea
            ? $"Agente en línea. Último contacto: {ultimo:dd/MM/yyyy HH:mm:ss}."
            : statusError
                ? $"Sin comunicación ({status}). Último contacto: {ultimo:dd/MM/yyyy HH:mm:ss}."
                : age.TotalMinutes > thresholdMinutes
                    ? $"Sin contacto reciente (>{thresholdMinutes} min). Último contacto: {ultimo:dd/MM/yyyy HH:mm:ss}."
                    : $"Estado: {status}. Último contacto: {ultimo:dd/MM/yyyy HH:mm:ss}.";

        return new AgenteComunicacionEpResponse
        {
            IdAtm = row.ID_ATM,
            KeepAlive = string.IsNullOrEmpty(keep) ? null : keep,
            Status = string.IsNullOrEmpty(status) ? null : status,
            FechaUltimaModificacion = ultimo,
            EnLinea = enLinea,
            Detalle = detalle
        };
    }
}
