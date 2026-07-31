using System.Text.Json.Serialization;

namespace ATT.Monitor.Api.Models.Atm;

public sealed class InsertComandoAtmRequest
{
    [JsonPropertyName("ID_CAJERO")]
    public string ID_CAJERO { get; set; } = string.Empty;

    [JsonPropertyName("ID_PAQUETE")]
    public int ID_PAQUETE { get; set; }

    [JsonPropertyName("COMANDO")]
    public string COMANDO { get; set; } = string.Empty;

    [JsonPropertyName("ID_USUARIO")]
    public string ID_USUARIO { get; set; } = string.Empty;

    /// <summary>Ruta en cajero (archivo a solicitar o carpeta/archivo destino al enviar). Opcional si va embebida en <see cref="COMANDO"/>.</summary>
    [JsonPropertyName("URL")]
    public string? URL { get; set; }
}

public sealed class EVersionAtm
{
    [JsonPropertyName("ID_ATM")]
    public string ID_ATM { get; set; } = string.Empty;

    [JsonPropertyName("VERSION")]
    public string VERSION { get; set; } = string.Empty;
}

public sealed class EstadoAtmRequest
{
    [JsonPropertyName("IdCajero")]
    public string IdCajero { get; set; } = string.Empty;

    [JsonPropertyName("Estado")]
    public string Estado { get; set; } = string.Empty;
}

public sealed class EIdSolComando
{
    [JsonPropertyName("idSolicitud")]
    public int? idSolicitud { get; set; }

    [JsonPropertyName("status")]
    public string status { get; set; } = string.Empty;
}

public sealed class CajeroAlarma
{
    [JsonPropertyName("IdCajero")]
    public string IdCajero { get; set; } = string.Empty;

    [JsonPropertyName("Dispositivo")]
    public int? Dispositivo { get; set; }

    [JsonPropertyName("Estado")]
    public int? Estado { get; set; }

    [JsonPropertyName("Alarma")]
    public int? Alarma { get; set; }
}

/// <summary>Identificador de cajero (paridad <c>EIdcajero</c> API v1).</summary>
public sealed class EIdcajero
{
    [JsonPropertyName("idcajero")]
    public string idcajero { get; set; } = string.Empty;
}

public sealed class SistemaInfo
{
    [JsonPropertyName("EP")]
    public string EP { get; set; } = string.Empty;

    [JsonPropertyName("SistemaOperativo")]
    public string SistemaOperativo { get; set; } = string.Empty;

    [JsonPropertyName("MemoriaRamGB")]
    public decimal MemoriaRamGB { get; set; }

    [JsonPropertyName("Idioma")]
    public string Idioma { get; set; } = string.Empty;

    [JsonPropertyName("Resolucion")]
    public string Resolucion { get; set; } = string.Empty;

    [JsonPropertyName("Procesador")]
    public string Procesador { get; set; } = string.Empty;

    [JsonPropertyName("ZonaHoraria")]
    public string ZonaHoraria { get; set; } = string.Empty;

    [JsonPropertyName("NombreEquipo")]
    public string NombreEquipo { get; set; } = string.Empty;

    [JsonPropertyName("AlmacenamientoGB")]
    public decimal AlmacenamientoGB { get; set; }
}
