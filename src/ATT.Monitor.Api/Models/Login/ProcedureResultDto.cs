namespace ATT.Monitor.Api.Models.Login;

/// <summary>Respuesta típica de SP con ResultInt / ResultString (paridad API v1).</summary>
public sealed class ProcedureResultDto
{
    public int? ResultInt { get; set; }
    public string ResultString { get; set; } = string.Empty;
}
