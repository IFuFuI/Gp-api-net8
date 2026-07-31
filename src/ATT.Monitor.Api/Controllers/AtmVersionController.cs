using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Atm;
using ATT.Monitor.Api.Models.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AtmVersionController(IAtmOperationsData atmOps) : ControllerBase
{
    [HttpPost("Update")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromBody] EVersionAtm request)
    {
        var result = await atmOps.InsertVersionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }
}
