using System.Globalization;
using ATT.Monitor.Api.Models.Conciliacion;

namespace ATT.Monitor.Api.Services;

/// <summary>Agregados sobre filas de transacciones para conciliación auxiliar (paridad lógica con Monitor Blazor).</summary>
public static class ConciliacionTransaccionalResumenCalculator
{
    /// <summary>Máximo de filas a pedir al SP (paridad con <c>ConciliacionTransaccionesAnalytics.MaxRowsToFetch</c> en Blazor).</summary>
    public const int MaxCantidadFila = 4000;

    public static IReadOnlyList<TransaccionOnlineRowApiDto> FilterByRegistrationRange(
        IReadOnlyList<TransaccionOnlineRowApiDto> rows,
        DateTime fechaInicioLocal,
        DateTime fechaFinLocal)
    {
        var start = fechaInicioLocal.Date;
        var end = fechaFinLocal.Date.AddDays(1).AddTicks(-1);

        return rows
            .Where(r => TryParseRowDate(r, out var dt) && dt >= start && dt <= end)
            .ToList();
    }

    public static bool TryParseRowDate(TransaccionOnlineRowApiDto row, out DateTime dateTime)
    {
        dateTime = default;
        var raw = row.FechaRegistro?.Trim();
        if (string.IsNullOrEmpty(raw))
            raw = row.Fecha?.Trim();
        if (string.IsNullOrEmpty(raw))
            return false;

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
        {
            dateTime = dt;
            return true;
        }

        if (DateTime.TryParse(raw, new CultureInfo("es-MX"), DateTimeStyles.AssumeLocal, out dt))
        {
            dateTime = dt;
            return true;
        }

        return false;
    }

    public static IReadOnlyList<ConciliacionEstatusBucketDto> CountByEstatus(IEnumerable<TransaccionOnlineRowApiDto> rows)
    {
        return rows
            .GroupBy(r => (r.Estatus ?? "—").Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ConciliacionEstatusBucketDto { Estatus = g.Key, Cantidad = g.Count() })
            .OrderByDescending(x => x.Cantidad)
            .ToList();
    }

    public static decimal SumMontos(IEnumerable<TransaccionOnlineRowApiDto> rows) =>
        rows.Sum(r => r.Monto ?? 0m);

    public static int CountWithErrorKeyword(IEnumerable<TransaccionOnlineRowApiDto> rows) =>
        rows.Count(r =>
        {
            var e = (r.Estatus ?? "").Trim();
            return e.Contains("ERR", StringComparison.OrdinalIgnoreCase)
                   || e.Contains("FALL", StringComparison.OrdinalIgnoreCase)
                   || e.Contains("RECH", StringComparison.OrdinalIgnoreCase);
        });
}
