namespace ATT.Monitor.Api.Services.Reportes;

internal static class ReporteEpFilterHelper
{
    public static string? BuildEpsCsv(IReadOnlyList<string>? eps, string? legacyEp)
    {
        var list = new List<string>();
        if (eps != null)
        {
            foreach (var raw in eps)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                var t = raw.Trim();
                if (!list.Contains(t, StringComparer.OrdinalIgnoreCase))
                    list.Add(t);
            }
        }

        if (list.Count == 0 && !string.IsNullOrWhiteSpace(legacyEp))
            list.Add(legacyEp.Trim());

        return list.Count == 0 ? null : string.Join(",", list);
    }

    public static IReadOnlyList<string> ResolveEpOrder(IReadOnlyList<string>? eps, string? legacyEp)
    {
        var csv = BuildEpsCsv(eps, legacyEp);
        if (string.IsNullOrWhiteSpace(csv))
            return Array.Empty<string>();

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
