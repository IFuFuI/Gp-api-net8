using System.Text.Json;
using System.Globalization;
using ATT.Monitor.Api.Models.Conciliacion;

namespace ATT.Monitor.Api.Services;

public static class AutopagoArchivoParser
{
    private const char Separador = '¬';          // ← corregido
    private const int  ColumnasEsperadas = 22;

     private static readonly JsonSerializerOptions JsonRead = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static (List<AutopagoDetalleRow> Filas, List<string> Errores) Parse(Stream stream)
    {
        var filas   = new List<AutopagoDetalleRow>();
        var errores = new List<string>();

        using var reader = new StreamReader(stream, leaveOpen: true);

        var header = reader.ReadLine();
        if (header is null)
        {
            errores.Add("El archivo está vacío.");
            return (filas, errores);
        }

        int lineNumber = 1;
        while (!reader.EndOfStream)
        {
            lineNumber++;
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // ← corregido: quitar comilla inicial del archivo
            if (line.StartsWith("\""))
                line = line.TrimStart('"');

            var cols = line.Split(Separador);
            if (cols.Length < ColumnasEsperadas)
            {
                errores.Add($"Línea {lineNumber}: se esperaban {ColumnasEsperadas} columnas, " +
                            $"se encontraron {cols.Length}.");
                continue;
            }

            filas.Add(new AutopagoDetalleRow
            {
                Region             = cols[0].Trim(),
                PosId              = cols[1].Trim(),
                Tienda             = cols[2].Trim(),
                Tipo               = cols[3].Trim(),
                OrdenCrmOms        = cols[4].Trim(),
                TipoDocumento      = cols[5].Trim(),
                NumeroDocumento    = cols[6].Trim(),
                ConceptoPago       = cols[7].Trim(),
                FormaPago          = cols[8].Trim(),
                NombreCliente      = cols[9].Trim(),
                TelefonoCuenta     = cols[10].Trim(),
                CuentaCliente      = cols[11].Trim(),
                Cajero             = cols[12].Trim(),
                IdentificadorCorte = cols[13].Trim(),
                PolizaGl           = cols[14].Trim(),
                EstatusCorte       = cols[15].Trim(),
                FechaEnvioPoliza   = cols[16].Trim(),
                Ticket             = cols[17].Trim(),
                Cancelado          = cols[18].Trim(),
                FechaTransaccion   = cols[19].Trim(),
                HoraTransaccion    = cols[20].Trim(),
                Importe            = ParseDecimal(cols[21]),
            });
        }

        return (filas, errores);
    }

    private static decimal? ParseDecimal(string val)
    {
        val = val.Trim();
        if (string.IsNullOrEmpty(val)) return null;

        if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;

        return null;
    }


public static IReadOnlyList<CargaConciliacionDto> ParseRows(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return Array.Empty<CargaConciliacionDto>();

        var trimmed = body.Trim();

        try
        {
            if (trimmed.StartsWith('['))
                return JsonSerializer.Deserialize<List<CargaConciliacionDto>>(trimmed, JsonRead)
                       ?? new List<CargaConciliacionDto>();

            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                var inner = doc.RootElement.GetString();
                if (string.IsNullOrWhiteSpace(inner))
                    return Array.Empty<CargaConciliacionDto>();
                inner = inner.Trim();
                if (inner.StartsWith('['))
                    return JsonSerializer.Deserialize<List<CargaConciliacionDto>>(inner, JsonRead)
                           ?? new List<CargaConciliacionDto>();
            }

            return Array.Empty<CargaConciliacionDto>();
        }
        catch (JsonException)
        {
            return Array.Empty<CargaConciliacionDto>();
        }
    }


}