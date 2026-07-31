using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Administrador;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>Paridad <c>api/Administrador</c> → <c>api/Administrador</c>.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AdministradorController(IAdministradorData administradorData) : ControllerBase
{
    [HttpPost("GetStatusBilletesAceptador")]
    [ProducesResponseType(typeof(IEnumerable<EstatusAceptadorBilletes>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusBilletesAceptador([FromBody] AceptadorRequest request)
    {
        var resp = await administradorData.GetStatusBilletesAceptadorAsync(
            request.PageNumber, request.PageSize, request.Buscar, request.OrderBy, request.OrderDir,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("ActualizarStatusBilletesAceptador")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActualizarStatusBilletesAceptador([FromBody] ActualizarAceptadorRequest request)
    {
        var resp = await administradorData.ActualizarStatusBilletesAceptadorAsync(
            request.ID, request.Descripcion, request.Severidad, request.ActivarAlerta,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("CrearStatusBilletesAceptador")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> CrearStatusBilletesAceptador([FromBody] CrearAceptadorRequest request)
    {
        var resp = await administradorData.CrearStatusBilletesAceptadorAsync(
            request.Descripcion, request.Severidad, request.ActivarAlerta,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("GetStatusMonedasAceptador")]
    [ProducesResponseType(typeof(IEnumerable<EstatusAceptadorMonedas>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusMonedasAceptador([FromBody] AceptadorRequest request)
    {
        var resp = await administradorData.GetStatusMonedasAceptadorAsync(
            request.PageNumber, request.PageSize, request.Buscar, request.OrderBy, request.OrderDir,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("ActualizarStatusMonedasAceptador")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActualizarStatusMonedasAceptador([FromBody] ActualizarAceptadorRequest request)
    {
        var resp = await administradorData.ActualizarStatusMonedasAceptadorAsync(
            request.ID, request.Descripcion, request.Severidad, request.ActivarAlerta,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("CrearStatusMonedasAceptador")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> CrearStatusMonedasAceptador([FromBody] CrearAceptadorRequest request)
    {
        var resp = await administradorData.CrearStatusMonedasAceptadorAsync(
            request.Descripcion, request.Severidad, request.ActivarAlerta,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("GetStatusBilletesDispensador")]
    [ProducesResponseType(typeof(IEnumerable<EstatusDispensadorBilletes>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusBilletesDispensador([FromBody] AceptadorRequest request)
    {
        var resp = await administradorData.GetStatusBilletesDispensadorAsync(
            request.PageNumber, request.PageSize, request.Buscar, request.OrderBy, request.OrderDir,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("ActualizarStatusBilletesDispensador")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActualizarStatusBilletesDispensador([FromBody] ActualizarAceptadorRequest request)
    {
        var resp = await administradorData.ActualizarStatusBilletesDispensadorAsync(
            request.ID, request.Descripcion, request.Severidad, request.ActivarAlerta,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("CrearStatusBilletesDispensador")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> CrearStatusBilletesDispensador([FromBody] CrearAceptadorRequest request)
    {
        var resp = await administradorData.CrearStatusBilletesDispensadorAsync(
            request.Descripcion, request.Severidad, request.ActivarAlerta,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("GetStatusMonedasDispensador")]
    [ProducesResponseType(typeof(IEnumerable<EstatusDispensadorMonedas>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusMonedasDispensador([FromBody] AceptadorRequest request)
    {
        var resp = await administradorData.GetStatusMonedasDispensadorAsync(
            request.PageNumber, request.PageSize, request.Buscar, request.OrderBy, request.OrderDir,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("ActualizarStatusMonedasDispensador")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActualizarStatusMonedasDispensador([FromBody] ActualizarDispensadorMonedasRequest request)
    {
        var resp = await administradorData.ActualizarStatusMonedasDispensadorAsync(
            request.ID, request.Nombre, request.Severidad,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("CrearStatusMonedasDispensador")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> CrearStatusMonedasDispensador([FromBody] CrearDispensadorMonedasRequest request)
    {
        var resp = await administradorData.CrearStatusMonedasDispensadorAsync(
            request.Nombre, request.Severidad,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("GetStatusImpresora")]
    [ProducesResponseType(typeof(IEnumerable<EstatusImpresora>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusImpresora([FromBody] AceptadorRequest request)
    {
        var resp = await administradorData.GetStatusImpresoraAsync(
            request.PageNumber, request.PageSize, request.Buscar, request.OrderBy, request.OrderDir,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("ActualizarStatusImpresora")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActualizarStatusImpresora([FromBody] ActualizarAceptadorRequest request)
    {
        var resp = await administradorData.ActualizarStatusImpresoraAsync(
            request.ID, request.Descripcion, request.Severidad, request.ActivarAlerta,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("CrearStatusImpresora")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> CrearStatusImpresora([FromBody] CrearAceptadorRequest request)
    {
        var resp = await administradorData.CrearStatusImpresoraAsync(
            request.Descripcion, request.Severidad, request.ActivarAlerta,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("GetStatusLectorCodigoBarras")]
    [ProducesResponseType(typeof(IEnumerable<EstatusLectorCodigoBarras>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusLectorCodigoBarras([FromBody] AceptadorRequest request)
    {
        var resp = await administradorData.GetStatusLectorCodigoBarrasAsync(
            request.PageNumber, request.PageSize, request.Buscar, request.OrderBy, request.OrderDir,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("ActualizarStatusLectorCodigoBarras")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActualizarStatusLectorCodigoBarras([FromBody] ActualizarAceptadorRequest request)
    {
        var resp = await administradorData.ActualizarStatusLectorCodigoBarrasAsync(
            request.ID, request.Descripcion, request.Severidad, request.ActivarAlerta,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    [HttpPost("CrearStatusLectorCodigoBarras")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> CrearStatusLectorCodigoBarras([FromBody] CrearAceptadorRequest request)
    {
        var resp = await administradorData.CrearStatusLectorCodigoBarrasAsync(
            request.Descripcion, request.Severidad, request.ActivarAlerta,
            HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }
}
