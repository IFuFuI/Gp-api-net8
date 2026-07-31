namespace ATT.Monitor.Api.Infrastructure.Persistence;

/// <summary>Embute la ruta en <c>COMANDO</c> cuando el SP no expone <c>@URL</c> (formato <c>COMANDO|ruta</c>).</summary>
internal static class AtmComandoPathCodec
{
    public const char Separator = '|';

    /// <summary>Comando limpio para columna <c>COMANDO</c> y ruta en <c>RUTA_CAJERO</c> / @URL (sin embeber pipe si hay ruta).</summary>
    public static (string Comando, string? Url) ApplyForStorage(string comando, string? url)
    {
        var (cmdFromRaw, pathInCmd) = Split(comando);
        var path = string.IsNullOrWhiteSpace(url) ? pathInCmd : url.Trim();
        var cmd = string.IsNullOrWhiteSpace(cmdFromRaw) ? (comando ?? string.Empty).Trim() : cmdFromRaw;

        if (string.IsNullOrEmpty(path))
            return (cmd, null);

        return (cmd, path);
    }

    public static string NormalizeGetComandoJson(string? jsonFromSp)
    {
        if (string.IsNullOrWhiteSpace(jsonFromSp))
            return jsonFromSp ?? string.Empty;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(jsonFromSp);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
                return jsonFromSp;

            if (!doc.RootElement.TryGetProperty("COMANDO", out var comandoEl))
                return jsonFromSp;

            var comandoRaw = comandoEl.GetString();
            var (cmd, path) = Split(comandoRaw);
            if (string.IsNullOrEmpty(path))
                return jsonFromSp;

            using var ms = new System.IO.MemoryStream();
            using (var writer = new System.Text.Json.Utf8JsonWriter(ms))
            {
                writer.WriteStartObject();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.NameEquals("COMANDO"))
                        writer.WriteString("COMANDO", cmd);
                    else if (prop.NameEquals("URL"))
                        writer.WriteString("URL", path);
                    else
                        prop.WriteTo(writer);
                }

                if (!doc.RootElement.TryGetProperty("URL", out _))
                    writer.WriteString("URL", path);

                writer.WriteEndObject();
            }

            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }
        catch
        {
            return jsonFromSp;
        }
    }

    public static (string Comando, string? Path) Split(string? comandoRaw)
    {
        var raw = (comandoRaw ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(raw))
            return (string.Empty, null);

        var idx = raw.IndexOf(Separator);
        if (idx <= 0)
            return (raw, null);

        var cmd = raw[..idx].Trim();
        var path = raw[(idx + 1)..].Trim();
        return (cmd, string.IsNullOrEmpty(path) ? null : path);
    }
}
