using ClosedXML.Excel;

namespace ATT.Monitor.Api.Services.Reportes;

internal static class ReportePlantillaExcelWriter
{
    public static async Task WriteAsync(
        string templatePath,
        string outputPath,
        int idReporte,
        string layoutSheetName,
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.Copy(templatePath, outputPath, overwrite: true);

        await Task.Run(() => FillWorkbook(outputPath, idReporte, layoutSheetName, rows), cancellationToken)
            .ConfigureAwait(false);
    }

    private static void FillWorkbook(
        string outputPath,
        int idReporte,
        string layoutSheetName,
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows)
    {
        using var workbook = new XLWorkbook(outputPath);
        var worksheet = workbook.Worksheets.FirstOrDefault(ws =>
                             string.Equals(ws.Name, layoutSheetName, StringComparison.OrdinalIgnoreCase))
                         ?? workbook.Worksheets.FirstOrDefault(ws =>
                             string.Equals(ws.Name, ReportePlantillaRegistry.LayoutSheetName,
                                 StringComparison.OrdinalIgnoreCase))
                         ?? throw new InvalidOperationException(
                             $"La plantilla no contiene la hoja «{layoutSheetName}».");

        var headerRow = worksheet.Row(1);
        var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 1;
        var headers = new List<string>(lastCol);
        for (var c = 1; c <= lastCol; c++)
            headers.Add(headerRow.Cell(c).GetString().Trim());

        while (headers.Count > 0 && string.IsNullOrWhiteSpace(headers[^1]))
            headers.RemoveAt(headers.Count - 1);

        if (headers.Count == 0)
            throw new InvalidOperationException("La hoja de layout no tiene encabezados en la fila 1.");

        var footerRow = FindFooterRow(worksheet, headers.Count);
        var dataEndRow = Math.Max(footerRow - 1, 1);

        ReportePlantillaHeaderFormatter.Apply(worksheet, headers.Count);

        var styleRow = worksheet.Row(2);
        var rowStyles = CaptureRowStyles(styleRow, headers.Count);

        if (dataEndRow >= 2)
            worksheet.Range(2, 1, dataEndRow, headers.Count).Clear(XLClearOptions.Contents);

        var writeRow = 2;
        foreach (var data in rows)
        {
            for (var c = 0; c < headers.Count; c++)
            {
                var header = headers[c];
                if (string.IsNullOrWhiteSpace(header))
                    continue;

                var cell = worksheet.Cell(writeRow, c + 1);
                cell.Style = ReportePlantillaHeaderFormatter.NeutralDataStyle(rowStyles[c]);
                cell.Style.Alignment.WrapText = false;

                var raw = ReportePlantillaColumnMapper.ResolveValue(idReporte, header, data);
                ReportePlantillaCellValueHelper.SetCellValue(cell, header, raw);
            }

            writeRow++;
        }

        if (writeRow > 2)
        {
            var dataRange = worksheet.Range(2, 1, writeRow - 1, headers.Count);
            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        RemoveWorksheetsExcept(workbook, worksheet);
        worksheet.SetTabActive();

        workbook.Save();
    }

    private static IXLStyle[] CaptureRowStyles(IXLRow styleRow, int columnCount)
    {
        var styles = new IXLStyle[columnCount];
        for (var c = 0; c < columnCount; c++)
            styles[c] = styleRow.Cell(c + 1).Style;
        return styles;
    }

    /// <summary>Deja un solo libro con la hoja de layout (sin pestaña legacy de la plantilla).</summary>
    private static void RemoveWorksheetsExcept(XLWorkbook workbook, IXLWorksheet keep)
    {
        foreach (var ws in workbook.Worksheets.Where(w => !ReferenceEquals(w, keep)).ToList())
            ws.Delete();
    }

    private static int FindFooterRow(IXLWorksheet worksheet, int columnCount)
    {
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= lastRow; r++)
        {
            var first = worksheet.Cell(r, 1).GetString().Trim();
            if (first.Length == 0)
                continue;

            if (first.StartsWith("Notas", StringComparison.OrdinalIgnoreCase) ||
                first.StartsWith("NOTA", StringComparison.OrdinalIgnoreCase))
                return r;
        }

        return lastRow + 1;
    }
}
