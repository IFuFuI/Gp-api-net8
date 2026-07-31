using System.Globalization;
using System.Text.RegularExpressions;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Configuration;
using ATT.Monitor.Api.Infrastructure.Persistence;
using ATT.Monitor.Api.Models.Dashboard;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ATT.Monitor.Api.Services.Reportes;

public sealed class ReporteGeneracionService(
    IDashboardData dashboard,
    IOptionsMonitor<ReporteHistoricoDescargaOptions> descargaOptions,
    IOptionsMonitor<MonitorFilePathsOptions> filePathsOptions,
    IOptionsMonitor<ReportePlantillasOptions> plantillasOptions,
    ReportePlantillaPathResolver plantillaPaths,
    IHostEnvironment hostEnvironment,
    ILogger<ReporteGeneracionService> logger) : IReporteGeneracionService
{
    public const int MaxRangoDiasInclusive = 365;
    public const int MaxFilasExport = 5000;
    private const int PageSize = 500;

    private sealed record ReporteDef(
        string StoredProcedure,
        IReadOnlyList<string> Columns,
        bool SoloError = false,
        string? XlsxTitle = null,
        string Orden = "FECHA");

    private static readonly IReadOnlyDictionary<int, ReporteDef> Definitions = BuildDefinitions();

    private static IReadOnlyDictionary<int, ReporteDef> BuildDefinitions()
    {
        var map = new Dictionary<int, ReporteDef>();
        foreach (var (id, sp, soloError, orden) in new (int Id, string Sp, bool SoloError, string Orden)[]
                 {
                     (1, "dbo.SP_GET_REPORTE_EP_ACTIVAS", false, "ID"),
                     (2, "dbo.SP_GET_REPORTE_TRANSACCIONES_BITACORA", true, "FECHA"),
                     (3, "dbo.SP_GET_REPORTE_TRANSACCIONES_POR_EQUIPO", false, "ID"),
                     (4, "dbo.SP_GET_REPORTE_CAMPANAS_MKT", false, "FECHA"),
                     (5, "dbo.SP_GET_REPORTE_CIERRE_CAJA", false, "FECHA"),
                     (6, "dbo.SP_GET_REPORTE_CONTADORES", false, "ID"),
                     (7, "dbo.SP_GET_REPORTE_TRANSACCIONES", false, "FECHA")
                 })
        {
            if (!ReporteExportColumnRegistry.TryGetExportColumns(id, out var columns))
                continue;

            map[id] = new ReporteDef(sp, columns, SoloError: soloError, Orden: orden);
        }

        return map;
    }

    public async Task<GenerarReporteResponse> GenerarAsync(
        GenerarReporteRequest request,
        string usuario,
        CancellationToken cancellationToken = default)
    {
        var prep = await dashboard.PrepararSolicitudReporteAsync(
            new PrepararSolicitudReporteRequest
            {
                IdReporte = request.IdReporte,
                FechaInicio = request.FechaInicio.Date,
                FechaFin = request.FechaFin.Date,
                Formato = request.Formato
            },
            cancellationToken).ConfigureAwait(false);

        if (!prep.Valido)
        {
            return new GenerarReporteResponse
            {
                Exito = false,
                Mensaje = prep.Mensaje
            };
        }

        if (!Definitions.TryGetValue(request.IdReporte, out var def))
        {
            return new GenerarReporteResponse
            {
                Exito = false,
                Mensaje =
                    "Tipo de reporte no soportado. Aplique API/db/2026_05_27_reportes_layout_siete_tipos.reference.sql en QA_MONITOR_ATT."
            };
        }

        var outputDir = ReporteHistoricoPathHelper.TryResolveOutputDirectory(
            descargaOptions.CurrentValue,
            filePathsOptions.CurrentValue,
            logger);
        if (outputDir is null)
        {
            return new GenerarReporteResponse
            {
                Exito = false,
                Mensaje =
                    "No hay carpeta de salida configurada (ReporteHistoricoDescarga:AllowedPathPrefixes o FileStorage:ArchivosAtmPath)."
            };
        }

        var fmt = prep.FormatoNormalizado ?? "csv";
        var descripcion = prep.DescripcionReporte ?? $"Reporte_{request.IdReporte}";
        var fileName = ReporteArchivoWriter.BuildFileName(descripcion, fmt);
        var fullPath = Path.Combine(outputDir, fileName);

        try
        {
            var (rows, filas) = await BuildFromStoredProcedureAsync(request, def, cancellationToken)
                .ConfigureAwait(false);

            if (filas == 0)
            {
                return new GenerarReporteResponse
                {
                    Exito = false,
                    Mensaje = "No hay datos en el rango y criterios seleccionados."
                };
            }

            var epOrder = request.IdReporte == 3
                ? ReporteEpFilterHelper.ResolveEpOrder(request.Eps, request.Ep)
                : null;
            PrepareExportRows(request.IdReporte, rows, epOrder);

            var usePlantilla = fmt.Equals("xlsx", StringComparison.OrdinalIgnoreCase) &&
                               plantillaPaths.TryResolveTemplatePath(request.IdReporte) is not null;

            var exportRows = usePlantilla
                ? rows.Select(CloneRow).ToList<IReadOnlyDictionary<string, string>>()
                : rows.Select(r => (IReadOnlyDictionary<string, string>)ProjectRow(request.IdReporte, r, def.Columns)).ToList();

            if (usePlantilla &&
                plantillaPaths.TryResolveTemplatePath(request.IdReporte) is { } templatePath)
            {
                var layoutSheet = ReportePlantillaRegistry.ResolveLayoutSheetName(
                    request.IdReporte,
                    plantillasOptions.CurrentValue.LayoutSheetName);

                await ReportePlantillaExcelWriter.WriteAsync(
                        templatePath,
                        fullPath,
                        request.IdReporte,
                        layoutSheet,
                        exportRows,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await ReporteArchivoWriter.WriteDictionaryRowsAsync(
                        fullPath,
                        fmt,
                        def.Columns,
                        exportRows,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            var idHist = await dashboard.InsertReporteHistoricoAsync(
                fileName,
                fmt,
                outputDir,
                string.IsNullOrWhiteSpace(usuario) ? "monitor" : usuario.Trim(),
                request.IdReporte,
                cancellationToken).ConfigureAwait(false);

            return new GenerarReporteResponse
            {
                Exito = true,
                Mensaje = idHist is > 0
                    ? $"Reporte generado ({filas:N0} fila(s)). Disponible en el historial."
                    : $"Archivo creado ({filas:N0} fila(s)); no se pudo registrar en historial.",
                IdReportesHistorico = idHist,
                NombreArchivo = fileName,
                FilasExportadas = filas
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al generar reporte id={IdReporte}", request.IdReporte);
            TryDeletePartial(fullPath);
            return new GenerarReporteResponse
            {
                Exito = false,
                Mensaje = BuildErrorMensaje(ex)
            };
        }
    }

    private async Task<(List<Dictionary<string, string>> Rows, int Filas)> BuildFromStoredProcedureAsync(
        GenerarReporteRequest request,
        ReporteDef def,
        CancellationToken cancellationToken)
    {
        var epsCsv = ReporteEpFilterHelper.BuildEpsCsv(request.Eps, request.Ep);
        var legacyEp = string.IsNullOrWhiteSpace(epsCsv) ? null : request.Ep?.Trim();

        var all = new List<Dictionary<string, string>>();
        var ignorar = 0;
        int? knownTotal = null;
        while (all.Count < MaxFilasExport)
        {
            var batch = await dashboard.GetReporteExportPaginadoAsync(
                new ReporteExportRequest
                {
                    StoredProcedure = def.StoredProcedure,
                    FechaInicio = request.FechaInicio.Date,
                    FechaFin = request.FechaFin.Date,
                    Ignorar = ignorar,
                    CantidadFila = PageSize,
                    SoloError = def.SoloError,
                    Ep = epsCsv == null ? legacyEp : null,
                    EpsCsv = epsCsv,
                    Orden = def.Orden,
                    Dir = "ASC"
                },
                cancellationToken).ConfigureAwait(false);

            if (batch.Count == 0)
                break;

            if (knownTotal == null && batch[0].TryGetValue("Total", out var t) &&
                int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tot))
                knownTotal = tot;

            foreach (var row in batch)
            {
                if (all.Count >= MaxFilasExport)
                    break;

                var copy = new Dictionary<string, string>(row, StringComparer.OrdinalIgnoreCase);
                copy.Remove("Total");
                MaskTarjetaInRow(copy);
                all.Add(copy);
            }

            ignorar += batch.Count;
            if (batch.Count < PageSize)
                break;
            if (knownTotal is > 0 && ignorar >= knownTotal.Value)
                break;
        }

        return (all, all.Count);
    }

    private static void PrepareExportRows(
        int idReporte,
        List<Dictionary<string, string>> rows,
        IReadOnlyList<string>? epOrder = null)
    {
        foreach (var row in rows)
        {
            row.Remove("Total");
            row.Remove("SortKey");
        }

        if (ReporteTransaccionesExportNormalizer.AppliesTo(idReporte))
            ReporteTransaccionesExportNormalizer.Apply(idReporte, rows);

        if (idReporte == 3)
            ReporteTransaccionesExportNormalizer.SortPorEquipo(rows, epOrder);
    }

    private static Dictionary<string, string> CloneRow(IReadOnlyDictionary<string, string> row) =>
        new(row, StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, string> ProjectRow(
        int idReporte,
        IReadOnlyDictionary<string, string> row,
        IReadOnlyList<string> columns)
    {
        var projected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var col in columns)
        {
            var raw = ReportePlantillaColumnMapper.ResolveValue(idReporte, col, row) ?? string.Empty;
            projected[col] = ReporteExportValueFormatter.FormatForExport(col, raw);
        }

        return projected;
    }

    private static void MaskTarjetaInRow(Dictionary<string, string> row)
    {
        foreach (var key in row.Keys.ToList())
        {
            if (key.Contains("Tarjeta", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("TARJETA", StringComparison.Ordinal))
            {
                row[key] = MaskTarjeta(row[key]);
            }
        }
    }

    private string BuildErrorMensaje(Exception ex)
    {
        if (ex is SqlException sql)
        {
            if (sql.Message.Contains("VW_BD_TRANSACCIONES_CONCILIACION", StringComparison.OrdinalIgnoreCase))
            {
                return "Falta la vista dbo.VW_BD_TRANSACCIONES_CONCILIACION en la base. " +
                       "Ejecute API/db/2026_05_19_reportes_vista_transacciones_conciliacion.reference.sql " +
                       "(o el script integral 2026_05_19_reportes_bitacora_seis_tipos.reference.sql).";
            }

            if (sql.Number is 2812 or 208)
            {
                return "Falta un objeto en SQL Server (procedimiento o vista de reportes). " +
                       "Aplique API/db/2026_05_19_reportes_bitacora_seis_tipos.reference.sql en QA_MONITOR_ATT.";
            }

            if (sql.Number == 9829)
            {
                return "El texto de estado de dispositivos superó el límite SQL. " +
                       "Ejecute API/db/2026_05_19_reportes_ep_tipo_falla_string_agg_fix.reference.sql en QA_MONITOR_ATT.";
            }

            if (hostEnvironment.IsDevelopment())
                return $"Error SQL ({sql.Number}): {sql.Message}";
        }

        if (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
        {
            return "No se pudo escribir el archivo. Verifique permisos en ReporteHistoricoDescarga / FileStorage:ArchivosAtmPath.";
        }

        if (hostEnvironment.IsDevelopment())
            return $"Error al generar: {ex.Message}";

        return "Error al generar el archivo. Revise permisos de carpeta, scripts SQL en API/db y logs del servidor.";
    }

    private static void TryDeletePartial(string fullPath)
    {
        try
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (Exception)
        {
            // ignore
        }
    }

    private static string MaskTarjeta(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;
        var digits = Regex.Replace(raw, @"\D", string.Empty);
        if (digits.Length < 4)
            return "****";
        return $"****{digits[^4..]}";
    }
}
