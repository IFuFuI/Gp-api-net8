using System.Text.Json;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Atm;
using ATT.Monitor.Api.Models.Crypto;
using ATT.Monitor.Api.Models.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>Paridad <c>api/AtmCommand</c> → <c>api/AtmCommand</c>.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AtmCommandController(IAtmOperationsData atmOps, IDashboardData dashboard, ICrypto crypto) : ControllerBase
{
    private static readonly JsonSerializerOptions IdCajeroJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [HttpPost("Get")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCommand([FromBody] EIdcajero request)
    {
        var result = await dashboard.ComandoAtmAsync(request.idcajero, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("GetEncrypted")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCommandEncrypted([FromBody] EncryptEnvelope? body)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.En))
            return BadRequest("Datos desencriptados inválidos.");

        string decrypted;
        try
        {
            decrypted = crypto.Decrypt(body.En);
        }
        catch (Exception)
        {
            return BadRequest("Datos desencriptados inválidos.");
        }

        if (string.IsNullOrWhiteSpace(decrypted))
            return BadRequest("Datos desencriptados inválidos.");

        var request = JsonSerializer.Deserialize<EIdcajero>(decrypted, IdCajeroJson);
        if (request is null || string.IsNullOrWhiteSpace(request.idcajero))
            return BadRequest("No se pudo deserializar la solicitud.");

        var result = await dashboard.ComandoAtmAsync(request.idcajero, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("UpdateStatus")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCommandStatus([FromBody] EIdSolComando request)
    {
        var result = await atmOps.UpdateComandoAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }
}
