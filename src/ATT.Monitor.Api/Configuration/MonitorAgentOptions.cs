namespace ATT.Monitor.Api.Configuration;

/// <summary>Umbral y reglas para comunicación agente ↔ Monitor (keep-alive).</summary>
public sealed class MonitorAgentOptions
{
    public const string SectionName = "MonitorAgent";

    /// <summary>Minutos sin contacto para considerar al agente fuera de línea.</summary>
    public int KeepAliveOnlineThresholdMinutes { get; set; } = 10;
}
