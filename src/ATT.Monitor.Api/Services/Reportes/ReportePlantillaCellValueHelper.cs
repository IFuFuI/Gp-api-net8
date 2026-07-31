using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace ATT.Monitor.Api.Services.Reportes;

internal static class ReportePlantillaCellValueHelper
{
    public const string DateDisplayFormat = "dd/MM/yyyy";
    public const string TimeDisplayFormat = "HH:mm:ss";

    private static readonly CultureInfo EsMx = CultureInfo.GetCultureInfo("es-MX");

    private static readonly string[] DateParseFormats =
    [
        "yyyy-MM-dd",
        "dd/MM/yyyy",
        "d/M/yyyy",
        "dd-MM-yyyy",
        "MM/dd/yyyy"
    ];

    public static bool IsDateColumn(string? header)
    {
        var key = ReporteExportValueFormatter.NormalizeHeaderKey(header);
        if (key.Length == 0)
            return false;

        if (key is "FECHA" or "FECHAINICIO" or "FECHAFIN" or "FECHATERMINO" or "FECHACREACION" or "FECHAULTIMAACT")
            return true;

        return key.StartsWith("FECHA", StringComparison.Ordinal);
    }

    public static bool IsTimeColumn(string? header)
    {
        var key = ReporteExportValueFormatter.NormalizeHeaderKey(header);
        return key is "HORA" or "HORAS";
    }

    public static void SetCellValue(IXLCell cell, string? header, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            cell.Value = Blank.Value;
            ApplyColumnFormat(cell, header);
            return;
        }

        var trimmed = raw.Trim();

        if (ReporteExportValueFormatter.NormalizeHeaderKey(header) is "ID")
            trimmed = StripLeadingAlphaPrefixFromId(trimmed);

        if (IsTimeColumn(header))
        {
            if (TryParseTime(trimmed, out var time))
            {
                cell.Value = time;
                cell.Style.DateFormat.Format = TimeDisplayFormat;
                return;
            }

            cell.SetValue(trimmed);
            ApplyColumnFormat(cell, header);
            return;
        }

        if (IsDateColumn(header))
        {
            if (TryParseReportDate(trimmed, out var date))
            {
                cell.Value = date.Date;
                cell.Style.DateFormat.Format = DateDisplayFormat;
                return;
            }

            cell.SetValue(trimmed);
            cell.Style.DateFormat.Format = DateDisplayFormat;
            return;
        }

        if (ReporteExportValueFormatter.IsStringColumn(header))
        {
            cell.SetValue(trimmed);
            cell.Style.NumberFormat.Format = "@";
            return;
        }

        if (ReporteExportValueFormatter.IsTipoOperacionColumn(header))
        {
            cell.SetValue(ReporteExportValueFormatter.FormatTipoOperacion(trimmed));
            return;
        }

        if (ReporteExportValueFormatter.IsCurrencyColumn(header) &&
            ReporteExportValueFormatter.TryParseDecimal(trimmed, out var currency))
        {
            cell.Value = currency;
            cell.Style.NumberFormat.Format = ReporteExportValueFormatter.ExcelCurrencyFormat;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            return;
        }

        if (ReporteExportValueFormatter.IsNumericColumn(header) &&
            ReporteExportValueFormatter.TryParseDecimal(trimmed, out var number))
        {
            cell.Value = number;
            return;
        }

        cell.SetValue(trimmed);
    }

    public static void ApplyColumnFormat(IXLCell cell, string? header)
    {
        if (IsDateColumn(header))
            cell.Style.DateFormat.Format = DateDisplayFormat;
        else if (IsTimeColumn(header))
            cell.Style.DateFormat.Format = TimeDisplayFormat;
    }

    private static bool TryParseReportDate(string raw, out DateTime date)
    {
        if (DateTime.TryParseExact(raw, DateParseFormats, EsMx, DateTimeStyles.None, out date))
            return true;

        if (DateTime.TryParseExact(raw, DateParseFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;

        if (TryParseExcelSerialDate(raw, out date))
            return true;

        if (DateTime.TryParse(raw, EsMx, DateTimeStyles.None, out date))
            return true;

        date = default;
        return false;
    }

    private static bool TryParseExcelSerialDate(string raw, out DateTime date)
    {
        date = default;
        if (!ReporteExportValueFormatter.TryParseDecimal(raw, out var serial))
            return false;

        if (serial < 30_000 || serial > 80_000)
            return false;

        if (Math.Abs(serial - Math.Round(serial)) > 0.0001)
            return false;

        try
        {
            date = DateTime.FromOADate(serial);
            return date.Year is >= 1990 and <= 2100;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryParseTime(string raw, out TimeSpan time)
    {
        time = default;
        if (TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out time))
            return true;

        if (TimeSpan.TryParse(raw, EsMx, out time))
            return true;

        if (DateTime.TryParseExact(raw, ["HH:mm:ss", "H:mm:ss", "HH:mm"], EsMx, DateTimeStyles.None, out var dt))
        {
            time = dt.TimeOfDay;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Columna ID en plantillas: quita prefijos alfabéticos al inicio (AP9001 → 9001, EP0123 → 0123).
    /// </summary>
    internal static string StripLeadingAlphaPrefixFromId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var trimmed = value.Trim();
        var i = 0;
        while (i < trimmed.Length && char.IsLetter(trimmed[i]))
            i++;

        if (i == 0 || i >= trimmed.Length)
            return trimmed;

        return trimmed[i..].Trim();
    }
}
