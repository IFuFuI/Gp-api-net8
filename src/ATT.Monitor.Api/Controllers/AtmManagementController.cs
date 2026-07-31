using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Atm;
using ATT.Monitor.Api.Models.Dashboard;
using ATT.Monitor.Api.Models.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>Paridad <c>api/AtmManagement</c> → <c>api/AtmManagement</c>.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AtmManagementController(IAtmOperationsData atmOps, IDashboardData dashboard) : ControllerBase
{
    [HttpPost("Register")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register([FromBody] InsertAtmRequest atm)
    {
        var result = await dashboard.InsertAtmAsync(atm, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("RegisterCounters")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegisterCounters([FromBody] InsertAtmContadoresRequest request)
    {
        var result = await dashboard.InsertAtmContadoresAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("KeepAlive")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> KeepAlive([FromBody] KeepAliveRequest request)
    {
        var result = await dashboard.InsertKeepAliveAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("UpdateState")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateState([FromBody] EstadoAtmRequest request)
    {
        var result = await atmOps.PostEstadoAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result))
            return NotFound($"No se pudo actualizar el estado del cajero {request.IdCajero}");
        return Ok(result);
    }
}
