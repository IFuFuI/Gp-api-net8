using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Campana;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>Paridad <c>api/Campana</c> → <c>api/Campana</c>.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CampanaController(ICampanaData campanaData, ICampanaDocumentService documentService) : ControllerBase
{
    [HttpPost("GET_CAMPANAS")]
    [ProducesResponseType(typeof(IEnumerable<CampanaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCampanas([FromBody] CampanaRequest request)
    {
        var result = await campanaData.GetCampanasAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        if (result is null || !result.Any())
            return NotFound(new { mensaje = "No se encontraron campañas." });
        return Ok(result);
    }

    [HttpPost("GET_CAMPANAS_EP")]
    [ProducesResponseType(typeof(IEnumerable<ConsultaEpCampanaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCampanasEP([FromBody] CampanaEPRequest request)
    {
        var result = await campanaData.GetCampanasEPAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        if (result is null)
            return Ok(Array.Empty<ConsultaEpCampanaResponse>());
        return Ok(result);
    }

    [HttpPost("AGREGAR_EPS_CAMPANA")]
    [ProducesResponseType(typeof(AgregarEpsCampanaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AgregarEpsCampana([FromBody] AgregarEpsCampanaRequest? request)
    {
        if (request is null || request.ID_CAMPANA <= 0)
            return BadRequest(new { mensaje = "ID de campaña inválido." });
        if (request.EP is null || !request.EP.Any())
            return BadRequest(new { mensaje = "Debe enviar al menos una EP." });

        var respuesta = new AgregarEpsCampanaResponse();

        foreach (var epRaw in request.EP.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var ep = (epRaw ?? string.Empty).Trim();
            if (ep.Length > 0)
                await AgregarEpAsync(request.ID_CAMPANA, ep, respuesta).ConfigureAwait(false);
        }

        respuesta.Mensaje = ResolverMensajeAgregado(respuesta);
        return Ok(respuesta);
    }

    /// <summary>Clasifica la EP en agregados, duplicados o fallidos segun el resultado de la insercion.</summary>
    private async Task AgregarEpAsync(int idCampana, string ep, AgregarEpsCampanaResponse respuesta)
    {
        if (await campanaData.EpExistsInCampanaAsync(idCampana, ep, HttpContext.RequestAborted).ConfigureAwait(false))
        {
            respuesta.OmitidosDuplicado.Add(ep);
            return;
        }

        var insertado = await campanaData.InsertCampanaEpAsync(idCampana, ep, HttpContext.RequestAborted).ConfigureAwait(false);
        if (insertado)
            respuesta.Agregados.Add(ep);
        else
            respuesta.Fallidos.Add(ep);
    }

    private static string ResolverMensajeAgregado(AgregarEpsCampanaResponse respuesta)
    {
        if (respuesta.Fallidos.Count > 0)
        {
            return respuesta.Agregados.Count == 0 && respuesta.OmitidosDuplicado.Count == 0
                ? "No se pudo agregar ninguna estación de pago."
                : "Algunas estaciones no se pudieron agregar; revise los detalles.";
        }

        if (respuesta.Agregados.Count == 0 && respuesta.OmitidosDuplicado.Count > 0)
            return "Todas las estaciones seleccionadas ya estaban en la campaña.";

        return "Proceso completado.";
    }

    [HttpPost("ELIMINAR_EP_CAMPANA")]
    [ProducesResponseType(typeof(CampanaSpResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EliminarEpCampana([FromBody] EliminarEpCampanaRequest? request)
    {
        if (request is null || request.ID_CAMPANA <= 0 || string.IsNullOrWhiteSpace(request.EP))
            return BadRequest(new { mensaje = "Datos inválidos." });

        var result = await campanaData.EliminarEpCampanaAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        if (result.ResultInt != 1)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("SubirCamp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Guardar([FromForm] NewCampanaRequest request)
    {
        if (request.archivo is null || request.archivo.Length == 0)
            return BadRequest("Debe enviar un archivo.");
        if (!Enum.IsDefined(typeof(TipoCampana), request.tipoCampana))
            return BadRequest("Tipo de campaña inválido. Debe ser PF o TA.");
        if (request.EP is null || !request.EP.Any())
            return BadRequest("Debe enviar al menos una EP");

        var resultado = await documentService.GuardarArchivoAsync(request.archivo, HttpContext.RequestAborted).ConfigureAwait(false);
        if (!resultado.Exito)
            return BadRequest(new { mensaje = resultado.Mensaje });

        var idArchivo = await campanaData.InsertArchivoCampanaAsync(
            resultado.RutaArchivo,
            resultado.NombreArchivo,
            HttpContext.RequestAborted).ConfigureAwait(false);

        var tipoTexto = request.tipoCampana.ToString();
        var idCampana = await campanaData.InsertCampanaAsync(
            request.nombreCampana,
            tipoTexto,
            request.fechainicio,
            request.fechafin,
            idArchivo,
            HttpContext.RequestAborted).ConfigureAwait(false);

        if (idCampana == 0)
            return StatusCode(StatusCodes.Status500InternalServerError, "Error al guardar campaña");

        foreach (var ep in request.EP)
        {
            var epInsertado = await campanaData.InsertCampanaEpAsync(idCampana, ep, HttpContext.RequestAborted).ConfigureAwait(false);
            if (!epInsertado)
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error al relacionar EP {ep}");
        }

        return Ok(new
        {
            mensaje = "Campaña creada para múltiples dispositivos 🚀",
            idCampana
        });
    }

    [HttpPut("BAJA_CAMPANA")]
    [ProducesResponseType(typeof(CampanaSpResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BajaCampana([FromBody] BajaCampanaRequest? request)
    {
        if (request is null || request.ID_CAMPANA <= 0)
            return BadRequest(new { mensaje = "Datos inválidos." });

        var result = await campanaData.BajaCampanaAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        if (result.ResultInt != 1)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("ACTUALIZAR_FECHAS_CAMPANA")]
    [ProducesResponseType(typeof(CampanaSpResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActualizarCampana([FromBody] ActualizarCampanaRequest? request)
    {
        if (request is null || request.ID_CAMPANA <= 0)
            return BadRequest(new { mensaje = "Datos inválidos." });

        var result = await campanaData.ActualizarCampanaAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        if (result.ResultInt != 1)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("ActualizarCamp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActualizarCamp([FromForm] UpdateCampanaZipRequest request)
    {
        if (request.archivo is null || request.archivo.Length == 0)
            return BadRequest("Debe enviar un archivo ZIP");

        var rutaZip = await campanaData.ObtenerRutaPorCampanaAsync(request.IdCampana, HttpContext.RequestAborted).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(rutaZip))
            return BadRequest("Ruta ZIP requerida");
        if (!System.IO.File.Exists(rutaZip))
            return NotFound("El ZIP no existe");

        var resultado = await documentService.ActualizarZipAsync(rutaZip, request.archivo, HttpContext.RequestAborted).ConfigureAwait(false);
        if (!resultado.Exito)
            return BadRequest(new { mensaje = resultado.Mensaje });

        var spResult = await campanaData.ActualizarZipCampanaAsync(request.IdCampana, HttpContext.RequestAborted).ConfigureAwait(false);
        if (spResult.ResultInt != 1)
            return BadRequest(spResult);

        return Ok(new { spResult.ResultInt, spResult.ResultString });
    }
}
