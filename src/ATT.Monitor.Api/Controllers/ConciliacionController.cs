using System.Globalization;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Conciliacion;
using ATT.Monitor.Api.Models.Dashboard;
using ATT.Monitor.Api.Services;
using static ATT.Monitor.Api.Services.AutopagoArchivoParser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ATT.Monitor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ConciliacionController(IConciliacion conciliacion, IDashboardData dashboard) : ControllerBase
{
    [HttpPost("resumen-transaccional")]
    [ProducesResponseType(typeof(ConciliacionResumenTransaccionalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResumenTransaccionalAsync(
        [FromBody] ConciliacionResumenTransaccionalRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryParseRangoFechas(request, out var ini, out var fin, out var error))
            return BadRequest(error);

        request ??= new ConciliacionResumenTransaccionalRequest();
        var cantidad = Math.Clamp(request.CantidadFila, 1, ConciliacionTransaccionalResumenCalculator.MaxCantidadFila);
        var filtro = string.IsNullOrWhiteSpace(request.Filtro) ? string.Empty : request.Filtro.Trim();

        var post = new PostTransaccion
        {
            Ignorar = Math.Max(0, request.Ignorar),
            Cantidad_Fila = cantidad,
            Filtro = filtro,
            Orden = string.IsNullOrWhiteSpace(request.Orden) ? "FECHA_REGISTRO" : request.Orden.Trim(),
            Dir = string.IsNullOrWhiteSpace(request.Dir) ? "DESC" : request.Dir.Trim()
        };

        var json = await dashboard.GetTransaccionesAsync(post, cancellationToken).ConfigureAwait(false);
        var rows = TransaccionesJsonParser.ParseRows(json);
        var filtered = ConciliacionTransaccionalResumenCalculator.FilterByRegistrationRange(rows, ini, fin);

        var response = new ConciliacionResumenTransaccionalResponse
        {
            FilasTraidasDeSp = rows.Count,
            MovimientosEnRango = filtered.Count,
            SumaMontos = ConciliacionTransaccionalResumenCalculator.SumMontos(filtered),
            PosiblesAnomaliasEstatus = ConciliacionTransaccionalResumenCalculator.CountWithErrorKeyword(filtered),
            PorEstatus = ConciliacionTransaccionalResumenCalculator.CountByEstatus(filtered).ToList(),
            FilasEnRango = filtered.ToList()
        };

        return Ok(response);
    }

    [HttpPost("resumen-conciliacion")]
    [ProducesResponseType(typeof(ConciliacionPbmxHistorialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResumenConcilacionAsync(
        [FromBody] ConciliacionPbmxHistorialRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new ConciliacionPbmxHistorialRequest();

        if (string.IsNullOrWhiteSpace(request.FechaInicio) || string.IsNullOrWhiteSpace(request.FechaFin))
            return BadRequest("fechaInicio y fechaFin son obligatorias (formato yyyy-MM-dd recomendado).");

        if (!DateTime.TryParse(request.FechaInicio, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _)
            && !DateTime.TryParse(request.FechaInicio, new CultureInfo("es-MX"), DateTimeStyles.AssumeLocal, out _))
            return BadRequest("fechaInicio no es una fecha válida.");

        if (!DateTime.TryParse(request.FechaFin, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var fin)
            && !DateTime.TryParse(request.FechaFin, new CultureInfo("es-MX"), DateTimeStyles.AssumeLocal, out fin))
            return BadRequest("fechaFin no es una fecha válida.");

        if (!DateTime.TryParse(request.FechaInicio, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var ini)
            && !DateTime.TryParse(request.FechaInicio, new CultureInfo("es-MX"), DateTimeStyles.AssumeLocal, out ini))
            return BadRequest("fechaInicio no es una fecha válida.");

        if (fin.Date < ini.Date)
            return BadRequest("fechaFin debe ser mayor o igual a fechaInicio.");

        var cantidad = Math.Clamp(request.CantidadFila, 1, 500);
        var filtro = string.IsNullOrWhiteSpace(request.Filtro) ? string.Empty : request.Filtro.Trim();

        var post = new ConciliacionResumenTransaccionalRequest
        {
            Ignorar = Math.Max(0, request.Ignorar),
            CantidadFila = cantidad,
            Filtro = filtro,
            Orden = "FECHA_CARGA",
            Dir = "DESC",
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin
        };

        var json = await conciliacion.GetConciliacionAsync(post, cancellationToken).ConfigureAwait(false);
        var cargas = ParseRows(json).ToList();

        var total = cargas.FirstOrDefault()?.Total ?? cargas.Count;
        var items = cargas.Select(c => MapHistorialItem(c, request.Tipo)).ToList();

        return Ok(new ConciliacionPbmxHistorialResponse
        {
            FilasTraidasDeSp = cargas.Count,
            Total = total,
            Items = items
        });
    }

    [HttpPost("cargar-archivo")]
    [ProducesResponseType(typeof(AutopagoCargaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CargarArchivoAsync(
        IFormFile? archivo,
        [FromForm] string usuarioCarga,
        CancellationToken cancellationToken)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest("El archivo es obligatorio.");

        if (string.IsNullOrWhiteSpace(usuarioCarga))
            return BadRequest("usuarioCarga es obligatorio.");

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (extension is not (".pbmx" or ".txt" or ".dat"))
            return BadRequest(new { mensaje = "Formato de archivo no válido. Use .pbmx, .txt o .dat." });

        using var stream = archivo.OpenReadStream();
        var (filas, errores) = AutopagoArchivoParser.Parse(stream);

        if (filas.Count == 0)
            return BadRequest(new { mensaje = "El archivo no contiene filas válidas.", errores });

        var hash = ComputeHash(archivo);

        var (idCarga, esReproceso, idCargaOrigen) = await conciliacion
            .RegistrarCargaAsync(archivo.FileName, hash, usuarioCarga.Trim(), cancellationToken)
            .ConfigureAwait(false);

        await conciliacion
            .InsertarDetalleAsync(idCarga, filas, cancellationToken)
            .ConfigureAwait(false);

        return Ok(new AutopagoCargaResponse
        {
            IdCarga = idCarga,
            EsReproceso = esReproceso,
            IdCargaOrigen = idCargaOrigen,
            FilasLeidas = filas.Count,
            FilasConError = errores.Count,
            Errores = errores
        });
    }

    [HttpGet("detalle/{idCarga:long}")]
    [ProducesResponseType(typeof(List<AutopagoDetalle>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DetalleConciliacionAsync(
        long idCarga,
        [FromQuery] bool soloDiferencias = false,
        CancellationToken cancellationToken = default)
    {
        if (idCarga <= 0)
            return BadRequest("idCarga debe ser un valor positivo.");

        var post = new IdCargaConciliacionResponse { IdCarga = (int)idCarga };

        var json = await conciliacion.GetDetalleConciliacionAsync(post, cancellationToken)
            .ConfigureAwait(false);

        var detalle = JsonSerializer.Deserialize<List<AutopagoDetalle>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];

        if (soloDiferencias)
        {
            detalle = detalle
                .Where(d => !string.Equals(d.ESTATUS_CONCILIACION, "CONCILIADO", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (detalle.Count == 0)
            return NotFound($"No se encontró información para la carga con id {idCarga}.");

        return Ok(detalle);
    }

    private static bool TryParseRangoFechas(
        ConciliacionResumenTransaccionalRequest? request,
        out DateTime ini,
        out DateTime fin,
        out string? error)
    {
        error = null;
        ini = default;
        fin = default;
        request ??= new ConciliacionResumenTransaccionalRequest();

        if (string.IsNullOrWhiteSpace(request.FechaInicio) || string.IsNullOrWhiteSpace(request.FechaFin))
        {
            error = "fechaInicio y fechaFin son obligatorias (formato yyyy-MM-dd recomendado).";
            return false;
        }

        if (!DateTime.TryParse(request.FechaInicio, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out ini)
            && !DateTime.TryParse(request.FechaInicio, new CultureInfo("es-MX"), DateTimeStyles.AssumeLocal, out ini))
        {
            error = "fechaInicio no es una fecha válida.";
            return false;
        }

        if (!DateTime.TryParse(request.FechaFin, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out fin)
            && !DateTime.TryParse(request.FechaFin, new CultureInfo("es-MX"), DateTimeStyles.AssumeLocal, out fin))
        {
            error = "fechaFin no es una fecha válida.";
            return false;
        }

        if (fin.Date < ini.Date)
        {
            error = "fechaFin debe ser mayor o igual a fechaInicio.";
            return false;
        }

        return true;
    }

    private static ConciliacionPbmxHistorialItemDto MapHistorialItem(CargaConciliacionDto c, string? tipo)
    {
        var fechaCarga = c.FECHA_CARGA;
        return new ConciliacionPbmxHistorialItemDto
        {
            IdCarga = c.ID_CARGA,
            Fecha = fechaCarga.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Hora = fechaCarga.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            Tipo = string.IsNullOrWhiteSpace(tipo) ? "EP" : tipo.Trim(),
            Archivo = c.NOMBRE_ARCHIVO ?? "-",
            Periodo = "-",
            Registros = c.TOTAL_REGISTROS,
            RegistrosConciliados = c.TOTAL_CONCILIADOS,
            Estado = c.ESTATUS_CARGA ?? "-",
            Usuario = c.USUARIO_CARGA ?? "-"
        };
    }

    private static string ComputeHash(IFormFile archivo)
    {
        using var stream = archivo.OpenReadStream();
        var bytes = System.Security.Cryptography.SHA256.HashData(stream);
        return Convert.ToHexString(bytes);
    }
}
