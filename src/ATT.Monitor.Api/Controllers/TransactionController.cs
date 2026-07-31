using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TransactionController(IDashboardData dashboard) : ControllerBase
{
    [HttpPost("Register")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegisterTransaction([FromBody] TransaccionRequest request)
    {
        var result = await dashboard.InsertTransaccionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("Transacciones")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Transaction([FromBody] PostTransaccion request)
    {
        var result = await dashboard.GetTransaccionesAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("TransaccionesEp")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> TransaccionesEp([FromBody] PostTransaccionEp request)
    {
        var result = await dashboard.GetTransaccionesEpAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result ?? "[]");
    }
}
