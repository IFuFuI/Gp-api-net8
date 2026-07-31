using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.TransArchivo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>Paridad <c>api/FilePackage</c> → <c>api/FilePackage</c>.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class FilePackageController(ITransArchivoData transArchivoData) : ControllerBase
{
    [HttpGet("{idCajero}")]
    [ProducesResponseType(typeof(MArchivo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPackage(string idCajero)
    {
        if (string.IsNullOrWhiteSpace(idCajero))
            return BadRequest("Parámetro 'idcajero' requerido.");

        var paquete = await transArchivoData.GetPaqueteAsync(idCajero, HttpContext.RequestAborted).ConfigureAwait(false);

        if (paquete is null)
            return NotFound("No se encontró información para el cajero especificado.");

        return Ok(paquete);
    }
}
