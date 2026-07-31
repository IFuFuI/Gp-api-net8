using System.Text.Json.Serialization;

namespace ATT.Monitor.Api.Models.JournalHistorico;

public sealed class JournalHistoricoExportJobCreateRequest
{
    [JsonPropertyName("ids")]
    public IReadOnlyList<string> Ids { get; set; } = Array.Empty<string>();

    [JsonPropertyName("unificar")]
    public bool Unificar { get; set; }

    [JsonPropertyName("allowedEps")]
    public IReadOnlyList<string>? AllowedEps { get; set; }
}

public sealed class JournalHistoricoExportJobCreateResponse
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public enum JournalHistoricoExportJobStatus
{
    Pending = 0,
    Processing = 1,
    Succeeded = 2,
    Failed = 3
}

public sealed class JournalHistoricoExportJobStatusDto
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public JournalHistoricoExportJobStatus Status { get; set; }

    [JsonPropertyName("createdUtc")]
    public DateTime CreatedUtc { get; set; }

    [JsonPropertyName("startedUtc")]
    public DateTime? StartedUtc { get; set; }

    [JsonPropertyName("finishedUtc")]
    public DateTime? FinishedUtc { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("downloadReady")]
    public bool DownloadReady { get; set; }

    [JsonPropertyName("summary")]
    public JournalHistoricoDownloadSummaryDto? Summary { get; set; }
}
