using System.Globalization;
using System.Text;

namespace ATT.Monitor.Api.Services.Reportes;

/// <summary>Mapea columnas del SP a encabezados de la hoja de layout (Nuevo Layout / Hoja1).</summary>
internal static class ReportePlantillaColumnMapper
{
    private static readonly IReadOnlyDictionary<string, string[]> SharedAliases =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["ESTACION DE PAGO"] = ["ESTACIÓN DE PAGO", "ESTACION DE PAGO"],
            ["REGION"] = ["REGIÓN", "REGION"],
            ["DIRECCION"] = ["DIRECCIÓN", "DIRECCION"],
            ["TICKET"] = ["FOLIO", "TICKET"],
            ["NOMBRE CLIENTE"] = ["NOMBRE CLIENTE", "NOMBRE DEL CLIENTE", "Nombre cliente"],
            ["TIPO DE OPERACION"] = ["TIPO DE OPERACIÓN", "TIPO DE OPERACION"],
            ["COD AUTORIZACION"] = ["COD. AUTORIZACIÓN", "CODIGO AUTORIZACION", "CODIGO_AUTORIZACION", "Codigo Aut"],
            ["NUM TARJETA"] = ["N° TARJETA", "NO_TARJETA", "NO TARJETA", "No de Tarjeta", "No de tarjeta"],
            ["NOMBRE CAMPANA"] = ["NOMBRE CAMPAÑA", "NOMBRE CAMPANA"],
            ["FECHA CREACION"] = ["FECHA CREACIÓN", "FECHA CREACION"],
            ["FECHA TERMINO"] = ["FECHA TERMINO", "FECHA FIN"],
            ["STATUS CAJA"] = ["Status Caja", "STATUS CAJA"],
            ["IMPRESORA"] = ["Impresora", "IMPRESORA"],
            ["CODI"] = ["Codi", "CODI"]
        };

    private static readonly IReadOnlyDictionary<int, IReadOnlyDictionary<string, string[]>> AliasesByReport =
        new Dictionary<int, IReadOnlyDictionary<string, string[]>>
        {
            [1] = SharedAliases,
            [2] = Merge(SharedAliases, TxTxnAliases()),
            [3] = Merge(SharedAliases, TxTxnAliases(), new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["METODO DE PAGO"] = ["CRITERIO DE PAGO", "METODO DE PAGO", "FORMA DE PAGO"]
            }),
            [4] = Merge(SharedAliases, new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["REGION"] = ["REGIÓN", "REGION"]
            }),
            [5] = Merge(SharedAliases, new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["REGION"] = ["REGIÓN", "REGION"]
            }),
            [6] = Merge(SharedAliases, new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["REGION"] = ["REGIÓN", "REGION"]
            }),
            [7] = Merge(SharedAliases, TxTxnAliases())
        };

    private static Dictionary<string, string[]> TxTxnAliases() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["CAMBIO PENDIENTE"] = ["CAMBIO PENDIENTE", "Cambio Incompleto", "CANTIDAD PENDIENTE"],
            ["CANTIDAD REVERSADA"] = ["CANTIDAD REVERSADA"]
        };

    private static Dictionary<string, string[]> Merge(
        params IReadOnlyDictionary<string, string[]>[] sources)
    {
        var merged = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            foreach (var kv in source)
                merged[kv.Key] = kv.Value;
        }

        return merged;
    }

    public static string? ResolveValue(
        int idReporte,
        string templateHeader,
        IReadOnlyDictionary<string, string> dataRow)
    {
        AliasesByReport.TryGetValue(idReporte, out var aliases);

        if (aliases is not null &&
            aliases.TryGetValue(templateHeader.Trim(), out var keys))
        {
            foreach (var key in keys)
            {
                if (TryGet(dataRow, key, out var v))
                    return v;
            }
        }

        if (TryGet(dataRow, templateHeader, out var direct))
            return direct;

        var normHeader = NormalizeKey(templateHeader);
        foreach (var kv in dataRow)
        {
            if (NormalizeKey(kv.Key) == normHeader)
                return kv.Value;
        }

        return null;
    }

    private static bool TryGet(IReadOnlyDictionary<string, string> row, string key, out string value)
    {
        if (row.TryGetValue(key, out var v))
        {
            value = v;
            return true;
        }

        var norm = NormalizeKey(key);
        foreach (var kv in row)
        {
            if (NormalizeKey(kv.Key) == norm)
            {
                value = kv.Value;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static string NormalizeKey(string key) =>
        ReporteExportValueFormatter.NormalizeHeaderKey(key);
}
