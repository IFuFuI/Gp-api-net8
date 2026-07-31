using System.Text.Json;
using ATT.Monitor.Api.Models.Conciliacion;

namespace ATT.Monitor.Api.Services;

/// <summary>Deserializa el JSON devuelto por <c>GetTransaccionesAsync</c> (string o string JSON-encoded).</summary>
public static class TransaccionesJsonParser
{
    private static readonly JsonSerializerOptions JsonRead = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<TransaccionOnlineRowApiDto> ParseRows(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return Array.Empty<TransaccionOnlineRowApiDto>();

        var trimmed = body.Trim();

        try
        {
            if (trimmed.StartsWith('['))
                return JsonSerializer.Deserialize<List<TransaccionOnlineRowApiDto>>(trimmed, JsonRead)
                       ?? new List<TransaccionOnlineRowApiDto>();

            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                var inner = doc.RootElement.GetString();
                if (string.IsNullOrWhiteSpace(inner))
                    return Array.Empty<TransaccionOnlineRowApiDto>();
                inner = inner.Trim();
                if (inner.StartsWith('['))
                    return JsonSerializer.Deserialize<List<TransaccionOnlineRowApiDto>>(inner, JsonRead)
                           ?? new List<TransaccionOnlineRowApiDto>();
            }

            return Array.Empty<TransaccionOnlineRowApiDto>();
        }
        catch (JsonException)
        {
            return Array.Empty<TransaccionOnlineRowApiDto>();
        }
    }
}
