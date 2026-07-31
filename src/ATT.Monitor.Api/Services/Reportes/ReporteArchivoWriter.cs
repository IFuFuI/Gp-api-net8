using System.Globalization;
using System.Text;
using MiniExcelLibs;

namespace ATT.Monitor.Api.Services.Reportes;

internal static class ReporteArchivoWriter
{
    public static async Task WriteDictionaryRowsAsync(
        string fullPath,
        string formato,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows,
        CancellationToken cancellationToken)
    {
        var fmt = formato.Equals("xlsx", StringComparison.OrdinalIgnoreCase) ? "xlsx" : "csv";
        if (fmt == "xlsx")
            await WriteXlsxAsync(fullPath, columns, rows, cancellationToken).ConfigureAwait(false);
        else
            await WriteCsvAsync(fullPath, columns, rows, cancellationToken).ConfigureAwait(false);
    }

    public static async Task WriteCsvAsync(
        string fullPath,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows,
        CancellationToken cancellationToken)
    {
        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await writer.WriteLineAsync(string.Join(',', columns.Select(CsvQuote))).ConfigureAwait(false);
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = string.Join(',', columns.Select(c => CsvQuote(GetCell(row, c))));
            await writer.WriteLineAsync(line).ConfigureAwait(false);
        }
    }

    public static async Task WriteXlsxWithTitleAsync(
        string fullPath,
        string title,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sheetRows = new List<Dictionary<string, object?>>(capacity: rows.Count + 2);

        var titleRow = columns.ToDictionary(
            c => c,
            c => (object?)(string.Equals(c, columns[0], StringComparison.OrdinalIgnoreCase) ? title : string.Empty),
            StringComparer.OrdinalIgnoreCase);
        sheetRows.Add(titleRow);
        sheetRows.Add(columns.ToDictionary(c => c, _ => (object?)string.Empty, StringComparer.OrdinalIgnoreCase));

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var d = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in columns)
                d[col] = GetCell(row, col);
            sheetRows.Add(d);
        }

        await using var ms = new MemoryStream();
        MiniExcel.SaveAs(ms, sheetRows, sheetName: "Reporte", excelType: ExcelType.XLSX);
        await File.WriteAllBytesAsync(fullPath, ms.ToArray(), cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteXlsxAsync(
        string fullPath,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dictRows = rows.Select(r =>
        {
            var d = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in columns)
                d[col] = GetCell(r, col);
            return d;
        }).ToList();

        await using var ms = new MemoryStream();
        MiniExcel.SaveAs(ms, dictRows, sheetName: "Reporte", excelType: ExcelType.XLSX);
        await File.WriteAllBytesAsync(fullPath, ms.ToArray(), cancellationToken).ConfigureAwait(false);
    }

    private static string GetCell(IReadOnlyDictionary<string, string> row, string column)
    {
        if (row.TryGetValue(column, out var v))
            return v;
        foreach (var kv in row)
        {
            if (string.Equals(kv.Key, column, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }

        return string.Empty;
    }

    private static string CsvQuote(string? s)
    {
        var t = (s ?? string.Empty).Replace("\"", "\"\"", StringComparison.Ordinal);
        return $"\"{t}\"";
    }

    public static string SanitizeFileStem(string value)
    {
        var stem = value.Trim();
        if (stem.Length == 0)
            stem = "reporte";
        foreach (var c in Path.GetInvalidFileNameChars())
            stem = stem.Replace(c, '_');
        if (stem.Length > 80)
            stem = stem[..80];
        return stem;
    }

    public static string BuildFileName(string descripcion, string extension)
    {
        var stem = SanitizeFileStem(descripcion);
        var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var ext = extension.Trim().TrimStart('.');
        return $"{stem}_{ts}.{ext}";
    }
}
