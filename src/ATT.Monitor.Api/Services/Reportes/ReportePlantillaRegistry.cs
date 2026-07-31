namespace ATT.Monitor.Api.Services.Reportes;

/// <summary>Catálogo de archivos plantilla por <c>IdReporte</c> (paridad ReportesMonitorSolicitud).</summary>
internal static class ReportePlantillaRegistry
{
    public const string LayoutSheetName = "Nuevo Layout";

    /// <summary>Reporte #3 no trae hoja &quot;Nuevo Layout&quot;; equivalente en plantilla actual.</summary>
    public const string TransaccionesPorEquipoLayoutSheet = "Hoja1";

    private static readonly IReadOnlyDictionary<int, string> TemplateFiles =
        new Dictionary<int, string>
        {
            [1] = "Reporte de Estaciones de Pago Activas.xlsx",
            [2] = "Reporte de Transacciones con Error.xlsx",
            [3] = "Reporte de Transacciones por Equipo.xlsx",
            [4] = "Reporte de Campañas MKT.xlsx",
            [5] = "Reporte de Cierre de Caja.xlsx",
            [6] = "Reporte de Contadores.xlsx",
            [7] = "Reporte de Transacciones.xlsx"
        };

    public static bool TryGetTemplateFileName(int idReporte, out string fileName) =>
        TemplateFiles.TryGetValue(idReporte, out fileName!);

    public static string ResolveLayoutSheetName(int idReporte, string defaultLayoutSheet)
    {
        if (idReporte == 3)
            return TransaccionesPorEquipoLayoutSheet;
        return defaultLayoutSheet;
    }
}
