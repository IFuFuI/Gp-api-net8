using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace ATT.Monitor.Api.Services.Reportes;

internal static class ReportePlantillaHeaderFormatter
{
    public static void Apply(IXLWorksheet worksheet, int columnCount)
    {
        for (var c = 1; c <= columnCount; c++)
        {
            var cell = worksheet.Cell(1, c);
            var raw = cell.GetString().Trim();
            if (raw.Length > 0)
                cell.Value = ToUppercaseNoAccent(raw);

            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Font.Bold = true;
        }
    }

    public static IXLStyle NeutralDataStyle(IXLStyle templateStyle)
    {
        var style = templateStyle;
        style.Fill.SetBackgroundColor(XLColor.NoColor);
        style.Font.Bold = false;
        return style;
    }

    internal static string ToUppercaseNoAccent(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(char.ToUpperInvariant(ch));
        }

        return sb.ToString();
    }
}
