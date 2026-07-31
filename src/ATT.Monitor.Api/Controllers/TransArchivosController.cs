using System.IO.Compression;
using System.Text.RegularExpressions;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.TransArchivo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>Paridad <c>api/TransArchivos</c> → <c>api/TransArchivos</c>.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TransArchivosController(
    ITransArchivoData transArchivo,
    IMonitorFilePaths filePaths) : ControllerBase
{
    private static readonly Regex InvalidFileCharsRegex = new(
        @"[^a-zA-Z0-9_-]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private const string MsgSinArchivo = "No se ha enviado ningún archivo.";
    private const string MsgSoloZip = "Solo se permiten archivos .zip";
    private const string MsgTipoRequerido = "Tipo de archivo requerido.";
    private const string MsgTipoInvalido = "Tipo de archivo contiene caracteres no válidos.";
    private const string MsgDirNoConfig = "Configuración de directorio de archivos no encontrada.";
    private const string MsgTipoArchivoRequerido = "El tipo de archivo es requerido.";
    private const string MsgDirNoEncontrado = "Directorio de archivos no encontrado.";
    private const string MsgParamInvalidos = "Parámetros inválidos.";
    private const string MsgInfoZipNoEncontrada = "No se encontró información del ZIP.";
    private const string MsgConfigDirNoEncontrada = "Configuración de directorio no encontrada.";
    private const string MsgZipNoExiste = "El archivo ZIP no existe en la ruta especificada.";
    private const string ZipExtension = ".zip";
    private const string MimeZip = "application/zip";
    private const int RandomLenSubirZip = 8;
    private const int RandomLenSubirZip2 = 9;
    private const string SearchPatternAll = "*";

    [HttpGet("GET_CATALOGO_ARCHIVO")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCatalogoArchivo()
    {
        var result = await transArchivo.GetCatalogoArchivoAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result))
            return NotFound("No se encontró información en el catálogo de archivo.");
        return Ok(result);
    }

    [HttpGet("GET_LIST_COMANDO")]
    [ProducesResponseType(typeof(IEnumerable<BitacoraComandoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBitacoraComandosAsync()
    {
        var result = await transArchivo.GetBitacoraComandosAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        if (result is null || !result.Any())
            return NotFound("No se encontraron registros en la bitácora de comandos.");
        return Ok(result);
    }

    [HttpGet("locations/json/raw")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLocationsJsonRaw()
    {
        var json = await transArchivo.GetLocationsJsonRawAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
            return NotFound("No se obtuvo información de ubicaciones.");
        return Content(json, "application/json");
    }

    [HttpGet("locations/{idLocation:int}/devices/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeviceConfigJson([FromRoute] int idLocation)
    {
        var json = await transArchivo.GetDeviceConfigJsonAsync(idLocation, HttpContext.RequestAborted).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
            return NotFound($"No se encontró configuración de dispositivos para Id_Location = {idLocation}");
        return Content(json, "application/json");
    }

    [HttpPost("SubirZip2")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SubirZip2([FromForm] ArchivoRequest n)
    {
        if (n.archivo is null || n.archivo.Length == 0)
            return BadRequest(MsgSinArchivo);
        if (!string.Equals(Path.GetExtension(n.archivo.FileName), ZipExtension, StringComparison.OrdinalIgnoreCase))
            return BadRequest(MsgSoloZip);
        if (string.IsNullOrWhiteSpace(n.tipoarchivo))
            return BadRequest(MsgTipoRequerido);

        var sanitizedTipoArchivo = InvalidFileCharsRegex.Replace(n.tipoarchivo, "");
        if (string.IsNullOrWhiteSpace(sanitizedTipoArchivo))
            return BadRequest(MsgTipoInvalido);

        var carpeta = filePaths.GetArchivosAtmRoot();
        if (string.IsNullOrWhiteSpace(carpeta))
            return BadRequest(MsgDirNoConfig);

        Directory.CreateDirectory(carpeta);

        var random = Path.GetRandomFileName().Replace(".", string.Empty, StringComparison.Ordinal);
        var baseName = random.Length >= RandomLenSubirZip2
            ? random[..RandomLenSubirZip2]
            : random;
        var uniqueName = $"{baseName}_{sanitizedTipoArchivo}{ZipExtension}";
        var destino = Path.Combine(carpeta, uniqueName);

        try
        {
            await using (var fs = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None))
                await n.archivo.CopyToAsync(fs, HttpContext.RequestAborted).ConfigureAwait(false);

            await transArchivo.InsertPaqueteAsync(n.idatm, uniqueName, sanitizedTipoArchivo, HttpContext.RequestAborted)
                .ConfigureAwait(false);

            return Ok(new { archivo = uniqueName });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error al guardar archivo: {ex.Message}");
        }
    }

    [HttpPost("SubirZip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SubirZip([FromForm] ArchivoRequest request)
    {
        var idSolicitud = 0;

        if (request.archivo is null || request.archivo.Length == 0)
            return BadRequest(MsgSinArchivo);
        if (!string.Equals(Path.GetExtension(request.archivo.FileName), ZipExtension, StringComparison.OrdinalIgnoreCase))
            return BadRequest(MsgSoloZip);
        if (string.IsNullOrWhiteSpace(request.tipoarchivo))
            return BadRequest(MsgTipoArchivoRequerido);

        try
        {
            var carpetaDestino = filePaths.GetArchivosAtmRoot();
            if (string.IsNullOrWhiteSpace(carpetaDestino))
                return BadRequest(MsgDirNoEncontrado);
            Directory.CreateDirectory(carpetaDestino);

            var nombreOriginal = Path.GetFileNameWithoutExtension(request.archivo.FileName);
            var partes = nombreOriginal.Split('_', 2);
            if (partes.Length > 0 && int.TryParse(partes[0], out var idExtraido))
                idSolicitud = idExtraido;

            var procesoEspecial = idSolicitud == 0;

            var extension = Path.GetExtension(request.archivo.FileName);
            var random = Path.GetRandomFileName().Replace(".", string.Empty, StringComparison.Ordinal);
            if (random.Length < RandomLenSubirZip)
                random = random.PadRight(RandomLenSubirZip, 'x');
            var nombreFinal = $"{nombreOriginal}_{random[..RandomLenSubirZip]}{extension}";
            var rutaFinal = Path.Combine(carpetaDestino, nombreFinal);

            await using (var fs = new FileStream(rutaFinal, FileMode.Create, FileAccess.Write, FileShare.None))
                await request.archivo.CopyToAsync(fs, HttpContext.RequestAborted).ConfigureAwait(false);

            var carpetaExtract = Path.Combine(carpetaDestino, Path.GetFileNameWithoutExtension(nombreFinal));

            if (procesoEspecial)
                await ExtraerJournalDiaDesdeZipAsync(request, nombreFinal).ConfigureAwait(false);

            string[] archivosExtraidos = [];
            var zipProtegido = false;

            if (request.APLICACION == 1)
            {
                var cmdResult = await transArchivo.InsertComandoAsync(request.idatm, 0, "DOWNLOAD", HttpContext.RequestAborted)
                    .ConfigureAwait(false);
                await transArchivo.InsertArchivoComandoAtmAsync(cmdResult.ResultInt, nombreFinal, HttpContext.RequestAborted)
                    .ConfigureAwait(false);
            }
            else
            {
                await transArchivo.InsertArchivoComandoAtmAsync(idSolicitud, nombreFinal, HttpContext.RequestAborted)
                    .ConfigureAwait(false);
            }

            try
            {
                ZipFile.ExtractToDirectory(rutaFinal, carpetaExtract);
                archivosExtraidos = Directory.GetFiles(carpetaExtract, SearchPatternAll, SearchOption.AllDirectories);

                await transArchivo.GuardarArchivosExtraidosAsync(
                    archivosExtraidos,
                    carpetaDestino,
                    nombreFinal,
                    1,
                    request.idatm,
                    request.APLICACION ?? 0,
                    HttpContext.RequestAborted).ConfigureAwait(false);
            }
            catch (InvalidDataException ex) when (
                ex.Message.Contains("unsupported compression method", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("encrypted", StringComparison.OrdinalIgnoreCase))
            {
                zipProtegido = true;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(carpetaExtract))
                        Directory.Delete(carpetaExtract, recursive: true);
                }
                catch
                {
                    // ignorar limpieza
                }
            }

            if (zipProtegido)
            {
                var archivoseguro = new List<string> { nombreFinal };
                await transArchivo.GuardarArchivosExtraidosAsync(
                    archivoseguro,
                    carpetaDestino,
                    nombreFinal,
                    1,
                    request.idatm,
                    request.APLICACION ?? 0,
                    HttpContext.RequestAborted).ConfigureAwait(false);

                return Ok(new
                {
                    zip = nombreFinal,
                    protegido = true,
                    mensaje = "El archivo ZIP está protegido con contraseña y no se pudo extraer su contenido."
                });
            }

            return Ok(new
            {
                zip = nombreFinal,
                totalArchivos = archivosExtraidos.Length,
                protegido = false
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error al procesar ZIP: {ex.Message}");
        }
    }

    [HttpPost("Descarga")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DescargarCatalogo([FromBody] EidAtm? e)
    {
        if (e is null)
            return BadRequest(MsgParamInvalidos);

        var info = await transArchivo.GetInfoZipAsync(e, HttpContext.RequestAborted).ConfigureAwait(false);
        if (info is null || string.IsNullOrWhiteSpace(info.NOMBRE_ZIP))
            return NotFound(MsgInfoZipNoEncontrada);

        var carpetaDestino = filePaths.GetArchivosAtmRoot();
        if (string.IsNullOrWhiteSpace(carpetaDestino))
            return BadRequest(MsgConfigDirNoEncontrada);

        var zipPath = Path.Combine(carpetaDestino, info.NOMBRE_ZIP);
        if (!System.IO.File.Exists(zipPath))
            return NotFound(MsgZipNoExiste);

        var fileBytes = await System.IO.File.ReadAllBytesAsync(zipPath, HttpContext.RequestAborted).ConfigureAwait(false);
        return File(fileBytes, MimeZip, info.NOMBRE_ZIP);
    }

    /// <summary>
    /// Journal diario en servidor: extrae el ZIP bajo <c>JournalDiaPath/{ep}/{lote}/</c> sin encolar comando ATM
    /// (evita <c>UPLOAD_JOURNALDIA</c> en GET_COMANDO). Preserva rutas internas del ZIP con mitigación ZipSlip.
    /// </summary>
    private async Task ExtraerJournalDiaDesdeZipAsync(ArchivoRequest request, string nombreZipEnArchivosAtm)
    {
        if (request.archivo is null || request.archivo.Length == 0)
            return;
        if (!string.Equals(Path.GetExtension(request.archivo.FileName), ".zip", StringComparison.OrdinalIgnoreCase))
            return;

        var journalRoot = filePaths.GetJournalDiaRoot();
        if (string.IsNullOrWhiteSpace(journalRoot))
            return;

        var epSeg = InvalidFileCharsRegex.Replace((request.idatm ?? string.Empty).Trim(), "_");
        if (string.IsNullOrWhiteSpace(epSeg))
            epSeg = "unknown";

        var loteSeg = InvalidFileCharsRegex.Replace(Path.GetFileNameWithoutExtension(nombreZipEnArchivosAtm), "_");
        if (string.IsNullOrWhiteSpace(loteSeg))
            loteSeg = "lote";

        var destDir = Path.Combine(journalRoot, epSeg, loteSeg);
        try
        {
            if (Directory.Exists(destDir))
            {
                Directory.Delete(destDir, recursive: true);
            }

            Directory.CreateDirectory(destDir);
            var destFull = Path.GetFullPath(destDir);
            if (!destFull.EndsWith(Path.DirectorySeparatorChar))
                destFull += Path.DirectorySeparatorChar;

            await using (var readStream = request.archivo.OpenReadStream())
            using (var zip = new ZipArchive(readStream, ZipArchiveMode.Read, leaveOpen: false))
            {
                foreach (var entry in zip.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    var entryPath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                    var combined = Path.GetFullPath(Path.Combine(destFull.TrimEnd(Path.DirectorySeparatorChar), entryPath));
                    if (!combined.StartsWith(destFull, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (Path.GetFileName(combined).Length == 0)
                        continue;

                    var parent = Path.GetDirectoryName(combined);
                    if (!string.IsNullOrEmpty(parent))
                        Directory.CreateDirectory(parent);

                    if (entry.Length == 0 && string.IsNullOrEmpty(Path.GetExtension(combined)))
                        continue;

                    entry.ExtractToFile(combined, overwrite: true);
                }
            }
        }
        catch
        {
            try
            {
                if (Directory.Exists(destDir))
                    Directory.Delete(destDir, recursive: true);
            }
            catch
            {
                // ignorar rollback
            }
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
