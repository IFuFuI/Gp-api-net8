using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Atm;
using ATT.Monitor.Api.Models.Dashboard;
using ATT.Monitor.Api.Models.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>Paridad <c>api/AtmDevice</c> → <c>api/AtmDevice</c>.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AtmDeviceController(IAtmOperationsData atmOps, IDashboardData dashboard) : ControllerBase
{
    [HttpPost("Status")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegisterDeviceStatus([FromBody] InsertDispositivoRequest request)
    {
        var result = await dashboard.InsertDispositivoAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("StatusByType")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegisterDeviceStatusByType([FromBody] InsertDispositivoRequestM request)
    {
        var result = await dashboard.InsertDispositivoAsyncTipo(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("HardwareStatus")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegisterHardwareStatus([FromBody] CajeroAlarma request)
    {
        var result = await atmOps.InsertStatusHwDispositivosAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }
}
