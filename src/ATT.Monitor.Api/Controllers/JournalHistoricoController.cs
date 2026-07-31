using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Infrastructure.Jobs;
using ATT.Monitor.Api.Models.JournalHistorico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>Listado y descarga de archivos journal diario en disco (<c>FileStorage:JournalDiaPath</c>).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class JournalHistoricoController(
    IJournalHistoricoService journalHistorico,
    IJournalHistoricoExportJobStore exportJobs,
    JournalHistoricoExportChannel exportChannel) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonSummary = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Listado paginado por EP y rango de fechas (UTC recomendado).</summary>
    [HttpPost("listar")]
    [ProducesResponseType(typeof(JournalHistoricoListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListarAsync([FromBody] JournalHistoricoListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var r = await journalHistorico.ListarAsync(request, cancellationToken).ConfigureAwait(false);
            return Ok(r);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Genera un ZIP con los archivos seleccionados; opcional unificación de texto.</summary>
    [HttpPost("descargar")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DescargarAsync([FromBody] JournalHistoricoDownloadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var (stream, fileName, summary, _) = await journalHistorico.CrearDescargaZipAsync(request, cancellationToken).ConfigureAwait(false);
            var summaryJson = JsonSerializer.Serialize(summary, JsonSummary);
            var summaryB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(summaryJson));
            Response.Headers.Append("X-Journal-Download-Summary", summaryB64);
            return File(stream, "application/zip", fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Encola generación de ZIP en segundo plano (un job activo por usuario).</summary>
    [HttpPost("export/jobs")]
    [ProducesResponseType(typeof(JournalHistoricoExportJobCreateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateExportJobAsync(
        [FromBody] JournalHistoricoExportJobCreateRequest body,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserKey();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (body.Ids is not { Count: > 0 })
            return BadRequest("Debe indicar al menos un archivo (ids).");

        var download = new JournalHistoricoDownloadRequest
        {
            Ids = body.Ids.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList(),
            Unificar = body.Unificar,
            AllowedEps = body.AllowedEps
        };

        if (!exportJobs.TryCreateJob(userId, download, out var jobId, out var rejection))
            return Conflict(new { message = rejection });

        await exportChannel.Writer.WriteAsync(jobId, cancellationToken).ConfigureAwait(false);

        return Ok(new JournalHistoricoExportJobCreateResponse
        {
            JobId = jobId,
            Message = "Exportación en cola. Puede navegar a otras pantallas; al volver podrá revisar el estado y descargar el ZIP."
        });
    }

    [HttpGet("export/jobs/active")]
    public IActionResult GetActiveExportJob()
    {
        var userId = ResolveUserKey();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var dto = exportJobs.GetActiveJobForUser(userId);
        return Ok(dto);
    }

    [HttpGet("export/jobs/{jobId}")]
    public IActionResult GetExportJobStatus(string jobId)
    {
        var userId = ResolveUserKey();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var dto = exportJobs.GetJob(userId, jobId);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("export/jobs/{jobId}/download")]
    public async Task<IActionResult> DownloadExportJobAsync(string jobId, CancellationToken cancellationToken)
    {
        var userId = ResolveUserKey();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var (stream, fileName) = await exportJobs.OpenDownloadAsync(userId, jobId, cancellationToken).ConfigureAwait(false);
        if (stream is null)
            return NotFound();

        return File(stream, "application/zip", fileName);
    }

    private string? ResolveUserKey()
        => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
}
