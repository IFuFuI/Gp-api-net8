using System.Globalization;
using System.Text;

namespace ATT.Monitor.Api.Services.Reportes;

/// <summary>Reglas de negocio transacciones en exportación (reportes #2, #3, #7).</summary>
internal static class ReporteTransaccionesExportNormalizer
{
    private static readonly int[] TransaccionesReportIds = [2, 3, 7];

    public static bool AppliesTo(int idReporte) =>
        TransaccionesReportIds.Contains(idReporte);

    public static void Apply(int idReporte, IList<Dictionary<string, string>> rows)
    {
        if (!AppliesTo(idReporte) || rows.Count == 0)
            return;

        for (var i = rows.Count - 1; i >= 0; i--)
        {
            if (IsExcludedReinicioEstatus(rows[i]))
                rows.RemoveAt(i);
        }

        foreach (var row in rows)
            ApplyBilleteAtorado(row);

        DeduplicateFoliosOnFailures(rows);
    }

    public static void SortPorEquipo(
        IList<Dictionary<string, string>> rows,
        IReadOnlyList<string>? epOrder = null)
    {
        if (rows.Count <= 1)
            return;

        IOrderedEnumerable<Dictionary<string, string>> ordered;
        if (epOrder is { Count: > 0 })
        {
            var rank = epOrder
                .Select((ep, i) => (Key: ep.Trim().ToUpperInvariant(), Index: i))
                .Where(x => x.Key.Length > 0)
                .GroupBy(x => x.Key)
                .ToDictionary(g => g.Key, g => g.First().Index);

            ordered = rows
                .OrderBy(r => rank.GetValueOrDefault(GetSortEp(r).ToUpperInvariant(), int.MaxValue))
                .ThenBy(r => GetSortFecha(r), StringComparer.Ordinal)
                .ThenBy(r => GetSortHora(r), StringComparer.Ordinal)
                .ThenBy(r => GetSortIdTxn(r));
        }
        else
        {
            ordered = rows
                .OrderBy(r => GetSortEp(r), StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => GetSortFecha(r), StringComparer.Ordinal)
                .ThenBy(r => GetSortHora(r), StringComparer.Ordinal)
                .ThenBy(r => GetSortIdTxn(r));
        }

        var sorted = ordered.ToList();

        rows.Clear();
        foreach (var row in sorted)
            rows.Add(row);
    }

    private static bool IsExcludedReinicioEstatus(IReadOnlyDictionary<string, string> row)
    {
        var estatus = GetEstatusRaw(row);
        return IsReinicioAplicativo(estatus) || IsReinicioEstacionPago(estatus);
    }

    private static bool IsReinicioAplicativo(string estatus) =>
        estatus.Contains("reinicio", StringComparison.OrdinalIgnoreCase) &&
        estatus.Contains("aplicativo", StringComparison.OrdinalIgnoreCase);

    private static bool IsReinicioEstacionPago(string estatus)
    {
        if (string.IsNullOrWhiteSpace(estatus))
            return false;

        var n = Normalize(estatus);
        return n.Contains("REINICIO", StringComparison.Ordinal) &&
               n.Contains("ESTACION", StringComparison.Ordinal) &&
               n.Contains("PAGO", StringComparison.Ordinal);
    }

    private static void ApplyBilleteAtorado(Dictionary<string, string> row)
    {
        var estatus = GetEstatusRaw(row);
        if (!IsBilleteAtorado(estatus))
            return;

        SetField(row, "ESTATUS", "Billete atorado");
        SetField(row, "CODIGO ERROR", "111:213");
    }

    private static bool IsBilleteAtorado(string estatus)
    {
        if (string.IsNullOrWhiteSpace(estatus))
            return false;

        var n = Normalize(estatus);
        return n.Contains("BILLETE", StringComparison.Ordinal) &&
               (n.Contains("ATORAD", StringComparison.Ordinal) || n.Contains("JAM", StringComparison.Ordinal));
    }

    private static void DeduplicateFoliosOnFailures(IList<Dictionary<string, string>> rows)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = rows.Count - 1; i >= 0; i--)
        {
            var folio = GetFolio(rows[i]);
            if (string.IsNullOrWhiteSpace(folio) || !IsFailureRow(rows[i]))
                continue;

            if (!seen.Add(folio))
                rows.RemoveAt(i);
        }
    }

    private static bool IsFailureRow(IReadOnlyDictionary<string, string> row)
    {
        var codigo = GetField(row, "CODIGO ERROR");
        if (!string.IsNullOrWhiteSpace(codigo) && codigo is not "-" and not " ")
            return true;

        var estatus = GetEstatusRaw(row);
        return estatus.Contains("error", StringComparison.OrdinalIgnoreCase) ||
               estatus.Contains("rechaz", StringComparison.OrdinalIgnoreCase) ||
               estatus.Contains("fall", StringComparison.OrdinalIgnoreCase) ||
               IsBilleteAtorado(estatus);
    }

    private static string GetEstatusRaw(IReadOnlyDictionary<string, string> row) =>
        GetField(row, "ESTATUS");

    private static string GetFolio(IReadOnlyDictionary<string, string> row) =>
        GetField(row, "FOLIO");

    private static string GetSortEp(IReadOnlyDictionary<string, string> row) =>
        GetField(row, "ID");

    private static string GetSortFecha(IReadOnlyDictionary<string, string> row) =>
        GetField(row, "FECHA");

    private static string GetSortHora(IReadOnlyDictionary<string, string> row) =>
        GetField(row, "HORA");

    private static long GetSortIdTxn(IReadOnlyDictionary<string, string> row)
    {
        if (row.TryGetValue("SortKey", out var sk) &&
            long.TryParse(sk, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            return id;
        return 0;
    }

    private static string GetField(IReadOnlyDictionary<string, string> row, string key)
    {
        if (row.TryGetValue(key, out var v))
            return v?.Trim() ?? string.Empty;

        var norm = Normalize(key);
        foreach (var kv in row)
        {
            if (Normalize(kv.Key) == norm)
                return kv.Value?.Trim() ?? string.Empty;
        }

        return string.Empty;
    }

    private static void SetField(Dictionary<string, string> row, string key, string value)
    {
        if (row.ContainsKey(key))
            row[key] = value;
        else
            row[key] = value;

        var norm = Normalize(key);
        foreach (var existing in row.Keys.ToList())
        {
            if (Normalize(existing) == norm)
                row[existing] = value;
        }
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            if (!char.IsWhiteSpace(ch))
                sb.Append(char.ToUpperInvariant(ch));
        }

        return sb.ToString();
    }
}
