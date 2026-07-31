using System.Text.Json.Serialization;

namespace ATT.Monitor.Api.Models.JournalHistorico;

public sealed class JournalHistoricoListRequest
{
    /// <summary>Compatibilidad mono-EP. Si se envía <see cref="Eps"/> con elementos se ignora en favor de esa lista.</summary>
    [JsonPropertyName("ep")]
    public string? Ep { get; set; }

    /// <summary>Una o más estaciones de pago para el listado combinado.</summary>
    [JsonPropertyName("eps")]
    public IReadOnlyList<string>? Eps { get; set; }

    [JsonPropertyName("desde")]
    public DateTime Desde { get; set; }

    [JsonPropertyName("hasta")]
    public DateTime Hasta { get; set; }

    [JsonPropertyName("pagina")]
    public int Pagina { get; set; }

    [JsonPropertyName("tamPagina")]
    public int TamPagina { get; set; } = 25;
}

public sealed class JournalHistoricoListResponse
{
    [JsonPropertyName("items")]
    public IReadOnlyList<JournalHistoricoItemDto> Items { get; set; } = Array.Empty<JournalHistoricoItemDto>();

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("rangoMaximoDias")]
    public int RangoMaximoDias { get; set; }
}

public sealed class JournalHistoricoItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("ep")]
    public string Ep { get; set; } = string.Empty;

    [JsonPropertyName("loteId")]
    public string LoteId { get; set; } = string.Empty;

    [JsonPropertyName("nombreArchivo")]
    public string NombreArchivo { get; set; } = string.Empty;

    [JsonPropertyName("rutaRelativa")]
    public string RutaRelativa { get; set; } = string.Empty;

    [JsonPropertyName("tamanoBytes")]
    public long TamanoBytes { get; set; }

    [JsonPropertyName("modificadoUtc")]
    public DateTime ModificadoUtc { get; set; }
}

public sealed class JournalHistoricoDownloadRequest
{
    /// <summary>Compatibilidad. Si falta se infiere desde cada id (multi-EP permitido).</summary>
    [JsonPropertyName("ep")]
    public string? Ep { get; set; }

    /// <summary>Opcional: restringir ids sólo si el EP coincide (mitiga manipulación de ids).</summary>
    [JsonPropertyName("allowedEps")]
    public IReadOnlyList<string>? AllowedEps { get; set; }

    [JsonPropertyName("ids")]
    public IReadOnlyList<string> Ids { get; set; } = Array.Empty<string>();

    [JsonPropertyName("unificar")]
    public bool Unificar { get; set; }
}

/// <summary>Metadatos breves para cabecera HTTP o consumo por el cliente.</summary>
public sealed class JournalHistoricoDownloadSummaryDto
{
    [JsonPropertyName("nombreZip")]
    public string NombreZip { get; set; } = string.Empty;

    [JsonPropertyName("ep")]
    public string Ep { get; set; } = string.Empty;

    [JsonPropertyName("archivosIncluidos")]
    public IReadOnlyList<string> ArchivosIncluidos { get; set; } = Array.Empty<string>();

    [JsonPropertyName("modo")]
    public string Modo { get; set; } = string.Empty;

    [JsonPropertyName("partes")]
    public int Partes { get; set; } = 1;
}
