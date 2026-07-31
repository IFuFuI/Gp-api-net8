namespace ATT.Monitor.Api.Configuration;

/// <summary>Rutas a plantillas XLSX (<c>ReportesMonitorSolicitud</c>).</summary>
public sealed class ReportePlantillasOptions
{
    public const string SectionName = "ReportePlantillas";

    /// <summary>
    /// Carpeta con los .xlsx de referencia. Si está vacío, se usa
    /// <c>{ContentRoot}/Templates/Reportes</c>.
    /// </summary>
    public string? TemplatesDirectory { get; set; }

    /// <summary>Nombre de la hoja con el layout vigente (mayoría de plantillas).</summary>
    public string LayoutSheetName { get; set; } = "Nuevo Layout";
}
