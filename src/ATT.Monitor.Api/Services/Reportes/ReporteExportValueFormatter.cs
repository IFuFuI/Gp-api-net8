using System.Globalization;
using System.Text;

namespace ATT.Monitor.Api.Services.Reportes;

/// <summary>Formateo de valores de exportación (CSV y celdas XLSX).</summary>
internal static class ReporteExportValueFormatter
{
    public const string ExcelCurrencyFormat = "\"$\"#,##0.00";

    private static readonly CultureInfo EsMx = CultureInfo.GetCultureInfo("es-MX");

    public static string FormatForExport(string? header, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var trimmed = raw.Trim();

        if (IsIdColumn(header))
            trimmed = ReportePlantillaCellValueHelper.StripLeadingAlphaPrefixFromId(trimmed);

        if (IsStringColumn(header))
            return trimmed;

        if (IsTipoOperacionColumn(header))
            return FormatTipoOperacion(trimmed);

        if (IsCurrencyColumn(header) && TryParseDecimal(trimmed, out var amount))
            return amount.ToString("C2", EsMx);

        if (IsNumericColumn(header) && TryParseDecimal(trimmed, out var number))
            return number.ToString(EsMx);

        return trimmed;
    }

    public static bool IsStringColumn(string? header)
    {
        var key = NormalizeHeaderKey(header);
        return key is "CUENTA" or "DN" or "TICKET";
    }

    public static bool IsTipoOperacionColumn(string? header) =>
        NormalizeHeaderKey(header) is "TIPODEOPERACION";

    public static string FormatTipoOperacion(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var trimmed = raw.Trim();
        return trimmed switch
        {
            "1" => "PAGO DE SERVICIO",
            "2" => "RECARGA TIEMPO AIRE",
            _ => trimmed
        };
    }

    public static bool IsCurrencyColumn(string? header)
    {
        var key = NormalizeHeaderKey(header);
        if (key.Length == 0)
            return false;

        if (key.Contains("MONTO", StringComparison.Ordinal))
            return true;

        return key is "EFECTIVO" or "TARJETA" or "CODI"
            or "CAMBIOENTREGADO" or "CAMBIOPENDIENTE" or "CANTIDADREVERSADA";
    }

    public static bool IsNumericColumn(string? header)
    {
        var key = NormalizeHeaderKey(header);
        if (key.Length == 0)
            return false;

        if (IsStringColumn(header) || IsCurrencyColumn(header))
            return IsCurrencyColumn(header);

        return key.Contains("MXN", StringComparison.Ordinal) ||
               key.Contains("DISPENSADO", StringComparison.Ordinal) ||
               key.Contains("REMANENTE", StringComparison.Ordinal) ||
               key.Contains("ACEPTADO", StringComparison.Ordinal) ||
               key.Contains("RECHAZADO", StringComparison.Ordinal) ||
               key.Contains("CASETERO", StringComparison.Ordinal) ||
               key is "ID";
    }

    public static bool IsDateColumn(string? header) =>
        ReportePlantillaCellValueHelper.IsDateColumn(header);

    public static bool IsTimeColumn(string? header) =>
        ReportePlantillaCellValueHelper.IsTimeColumn(header);

    public static bool TryParseDecimal(string raw, out double number)
    {
        if (double.TryParse(raw, NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                CultureInfo.InvariantCulture, out number))
            return true;

        if (double.TryParse(raw, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, EsMx, out number))
            return true;

        return false;
    }

    private static bool IsIdColumn(string? header) => NormalizeHeaderKey(header) is "ID";

    internal static string NormalizeHeaderKey(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return string.Empty;

        var sb = new StringBuilder(header.Length);
        foreach (var ch in header.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToUpperInvariant(ch));
        }

        return sb.ToString();
    }
}
